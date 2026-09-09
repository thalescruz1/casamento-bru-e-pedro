using Casamento.Domain.Rsvps.ValueObjects;

namespace Casamento.Domain.Gifts.ValueObjects;

public sealed record Purchase(
    string BuyerName,
    Email BuyerEmail,
    string AsaasPaymentId,
    DateTimeOffset PaidAt,
    string? Message = null);
