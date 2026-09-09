using System.Net;
using Casamento.Application.Abstractions;
using Casamento.Domain.Gifts;
using Casamento.Infrastructure.Cosmos.Documents;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace Casamento.Infrastructure.Cosmos;

internal sealed class CosmosGiftRepository(
    CosmosClient client,
    IOptions<CosmosOptions> options) : IGiftRepository
{
    private const string PartitionKeyValue = "gift";
    private readonly Container _container = client.GetContainer(options.Value.DatabaseName, options.Value.GiftsContainer);

    public async Task AddAsync(Gift gift, CancellationToken cancellationToken)
    {
        var doc = GiftDocument.FromAggregate(gift);
        await _container.CreateItemAsync(doc, new PartitionKey(PartitionKeyValue), cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpdateAsync(Gift gift, CancellationToken cancellationToken)
    {
        var doc = GiftDocument.FromAggregate(gift);
        await _container.UpsertItemAsync(doc, new PartitionKey(PartitionKeyValue), cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _container.DeleteItemAsync<GiftDocument>(
                id.ToString("N"),
                new PartitionKey(PartitionKeyValue),
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
        }
    }

    public async Task<Gift?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _container.ReadItemAsync<GiftDocument>(
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

    public async Task<Gift?> FindByAsaasPaymentIdAsync(string asaasPaymentId, CancellationToken cancellationToken)
    {
        var query = new QueryDefinition(
            "SELECT TOP 1 * FROM c WHERE c.type = @type AND c.asaasPaymentId = @pid")
            .WithParameter("@type", PartitionKeyValue)
            .WithParameter("@pid", asaasPaymentId);

        using var iterator = _container.GetItemQueryIterator<GiftDocument>(query, requestOptions: new QueryRequestOptions
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

    public async Task<IReadOnlyList<Gift>> ListAsync(bool includeUnavailable, CancellationToken cancellationToken)
    {
        // status=1 Available · status=3 Paid · status=4 Canceled · status=2 (legacy Reserved) é tratado como Available em memória.
        var sql = includeUnavailable
            ? "SELECT * FROM c WHERE c.type = @type ORDER BY c.createdAt DESC"
            : "SELECT * FROM c WHERE c.type = @type AND (c.status = 1 OR c.status = 2) ORDER BY c.createdAt DESC";

        var query = new QueryDefinition(sql).WithParameter("@type", PartitionKeyValue);

        using var iterator = _container.GetItemQueryIterator<GiftDocument>(query, requestOptions: new QueryRequestOptions
        {
            PartitionKey = new PartitionKey(PartitionKeyValue)
        });

        var results = new List<Gift>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);
            results.AddRange(response.Select(d => d.ToAggregate()));
        }
        return results;
    }
}
