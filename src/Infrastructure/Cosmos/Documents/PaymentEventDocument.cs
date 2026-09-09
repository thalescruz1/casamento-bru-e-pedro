using Newtonsoft.Json;

namespace Casamento.Infrastructure.Cosmos.Documents;

internal sealed class PaymentEventDocument
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("giftId")]
    public string GiftId { get; set; } = string.Empty;

    [JsonProperty("eventType")]
    public string EventType { get; set; } = string.Empty;

    [JsonProperty("asaasPaymentId")]
    public string AsaasPaymentId { get; set; } = string.Empty;

    [JsonProperty("receivedAt")]
    public DateTimeOffset ReceivedAt { get; set; }

    [JsonProperty("rawPayload")]
    public string RawPayload { get; set; } = string.Empty;
}
