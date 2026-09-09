using System.Net;
using Casamento.Application.Abstractions;
using Casamento.Domain.Auth;
using Casamento.Infrastructure.Cosmos.Documents;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace Casamento.Infrastructure.Cosmos;

internal sealed class CosmosAdminUserRepository(
    CosmosClient client,
    IOptions<CosmosOptions> options) : IAdminUserRepository
{
    private const string PartitionKeyValue = "admin";
    private readonly Container _container = client.GetContainer(options.Value.DatabaseName, options.Value.AdminsContainer);

    public async Task<AdminUser?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var query = new QueryDefinition("SELECT TOP 1 * FROM c WHERE c.type = @type AND c.email = @email")
            .WithParameter("@type", PartitionKeyValue)
            .WithParameter("@email", normalized);

        using var iterator = _container.GetItemQueryIterator<AdminUserDocument>(query, requestOptions: new QueryRequestOptions
        {
            PartitionKey = new PartitionKey(PartitionKeyValue)
        });

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);
            var first = response.FirstOrDefault();
            if (first is not null)
            {
                return first.ToAggregate();
            }
        }
        return null;
    }

    public async Task<AdminUser?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _container.ReadItemAsync<AdminUserDocument>(
                id.ToString("N"),
                new PartitionKey(PartitionKeyValue),
                cancellationToken: cancellationToken).ConfigureAwait(false);
            return response.Resource.ToAggregate();
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<bool> AnyAsync(CancellationToken cancellationToken)
    {
        var query = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.type = @type")
            .WithParameter("@type", PartitionKeyValue);

        using var iterator = _container.GetItemQueryIterator<int>(query, requestOptions: new QueryRequestOptions
        {
            PartitionKey = new PartitionKey(PartitionKeyValue)
        });

        if (!iterator.HasMoreResults)
        {
            return false;
        }

        var response = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);
        return response.FirstOrDefault() > 0;
    }

    public async Task AddAsync(AdminUser user, CancellationToken cancellationToken)
    {
        var doc = AdminUserDocument.FromAggregate(user);
        await _container.CreateItemAsync(doc, new PartitionKey(PartitionKeyValue), cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpdateAsync(AdminUser user, CancellationToken cancellationToken)
    {
        var doc = AdminUserDocument.FromAggregate(user);
        await _container.UpsertItemAsync(doc, new PartitionKey(PartitionKeyValue), cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }
}
