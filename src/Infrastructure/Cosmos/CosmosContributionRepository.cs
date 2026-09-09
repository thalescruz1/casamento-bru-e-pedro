using System.Net;
using Casamento.Application.Abstractions;
using Casamento.Domain.Contributions;
using Casamento.Infrastructure.Cosmos.Documents;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace Casamento.Infrastructure.Cosmos;

internal sealed class CosmosContributionRepository(
    CosmosClient client,
    IOptions<CosmosOptions> options) : IContributionRepository
{
    private const string PartitionKeyValue = "contribution";
    private readonly Container _container = client.GetContainer(options.Value.DatabaseName, options.Value.ContributionsContainer);

    public async Task AddAsync(Contribution contribution, CancellationToken cancellationToken)
    {
        var doc = ContributionDocument.FromAggregate(contribution);
        await _container.CreateItemAsync(doc, new PartitionKey(PartitionKeyValue), cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpdateAsync(Contribution contribution, CancellationToken cancellationToken)
    {
        var doc = ContributionDocument.FromAggregate(contribution);
        await _container.UpsertItemAsync(doc, new PartitionKey(PartitionKeyValue), cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Contribution?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _container.ReadItemAsync<ContributionDocument>(
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

    public async Task<Contribution?> FindByAsaasPaymentIdAsync(string asaasPaymentId, CancellationToken cancellationToken)
    {
        var query = new QueryDefinition(
            "SELECT TOP 1 * FROM c WHERE c.type = @type AND c.asaasPaymentId = @pid")
            .WithParameter("@type", PartitionKeyValue)
            .WithParameter("@pid", asaasPaymentId);

        using var iterator = _container.GetItemQueryIterator<ContributionDocument>(query, requestOptions: new QueryRequestOptions
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

    public async Task<IReadOnlyList<Contribution>> ListAsync(CancellationToken cancellationToken)
    {
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.type = @type ORDER BY c.createdAt DESC")
            .WithParameter("@type", PartitionKeyValue);

        using var iterator = _container.GetItemQueryIterator<ContributionDocument>(query, requestOptions: new QueryRequestOptions
        {
            PartitionKey = new PartitionKey(PartitionKeyValue)
        });

        var results = new List<Contribution>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);
            results.AddRange(response.Select(d => d.ToAggregate()));
        }
        return results;
    }
}
