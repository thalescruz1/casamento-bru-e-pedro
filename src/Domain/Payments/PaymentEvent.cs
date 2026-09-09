namespace Casamento.Domain.Payments;

public sealed record PaymentEvent(
    string EventId,
    string EventType,
    string AsaasPaymentId,
    Guid? GiftId,
    DateTimeOffset ReceivedAt,
    string RawPayload);
