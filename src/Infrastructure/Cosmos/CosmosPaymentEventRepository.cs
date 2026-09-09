using System.Net;
using Casamento.Application.Abstractions;
using Casamento.Domain.Payments;
using Casamento.Infrastructure.Cosmos.Documents;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace Casamento.Infrastructure.Cosmos;

internal sealed class CosmosPaymentEventRepository(
    CosmosClient client,
    IOptions<CosmosOptions> options) : IPaymentEventRepository
{
    private readonly Container _container = client.GetContainer(options.Value.DatabaseName, options.Value.PaymentEventsContainer);

    public async Task<bool> TryRecordAsync(PaymentEvent evt, CancellationToken cancellationToken)
    {
        var giftIdPartition = evt.GiftId?.ToString("N") ?? "orphan";
        var doc = new PaymentEventDocument
        {
            Id = evt.EventId,
            GiftId = giftIdPartition,
            EventType = evt.EventType,
            AsaasPaymentId = evt.AsaasPaymentId,
            ReceivedAt = evt.ReceivedAt,
            RawPayload = evt.RawPayload
        };

        try
        {
            await _container.CreateItemAsync(
                doc,
                new PartitionKey(giftIdPartition),
                cancellationToken: cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            return false;
        }
    }
}
