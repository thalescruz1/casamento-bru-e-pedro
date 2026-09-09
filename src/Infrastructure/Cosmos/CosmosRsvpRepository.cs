using System.Net;
using Casamento.Application.Abstractions;
using Casamento.Domain.Rsvps;
using Casamento.Infrastructure.Cosmos.Documents;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace Casamento.Infrastructure.Cosmos;

internal sealed class CosmosRsvpRepository(
    CosmosClient client,
    IOptions<CosmosOptions> options) : IRsvpRepository
{
    private const string PartitionKeyValue = "rsvp";
    private readonly Container _container = client.GetContainer(options.Value.DatabaseName, options.Value.RsvpsContainer);

    public async Task AddAsync(Rsvp rsvp, CancellationToken cancellationToken)
    {
        var doc = RsvpDocument.FromAggregate(rsvp);
        await _container.CreateItemAsync(doc, new PartitionKey(PartitionKeyValue), cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<int> CountSubmissionsFromAsync(string ipHash, DateTimeOffset since, CancellationToken cancellationToken)
    {
        var query = new QueryDefinition(
            "SELECT VALUE COUNT(1) FROM c WHERE c.type = @type AND c.ipHash = @ipHash AND c.submittedAt >= @since")
            .WithParameter("@type", PartitionKeyValue)
            .WithParameter("@ipHash", ipHash)
            .WithParameter("@since", since);

        using var iterator = _container.GetItemQueryIterator<int>(query, requestOptions: new QueryRequestOptions
        {
            PartitionKey = new PartitionKey(PartitionKeyValue),
            MaxItemCount = 1
        });

        if (!iterator.HasMoreResults)
        {
            return 0;
        }

        var response = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);
        return response.FirstOrDefault();
    }

    public async Task<IReadOnlyList<Rsvp>> ListAsync(int skip, int take, CancellationToken cancellationToken)
    {
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.type = @type ORDER BY c.submittedAt DESC OFFSET @skip LIMIT @take")
            .WithParameter("@type", PartitionKeyValue)
            .WithParameter("@skip", skip)
            .WithParameter("@take", take);

        using var iterator = _container.GetItemQueryIterator<RsvpDocument>(query, requestOptions: new QueryRequestOptions
        {
            PartitionKey = new PartitionKey(PartitionKeyValue)
        });

        var results = new List<Rsvp>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);
            results.AddRange(response.Select(d => d.ToAggregate()));
        }
        return results;
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken)
    {
        var query = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.type = @type")
            .WithParameter("@type", PartitionKeyValue);

        using var iterator = _container.GetItemQueryIterator<int>(query, requestOptions: new QueryRequestOptions
        {
            PartitionKey = new PartitionKey(PartitionKeyValue)
        });

        if (!iterator.HasMoreResults)
        {
            return 0;
        }

        var response = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);
        return response.FirstOrDefault();
    }
}
