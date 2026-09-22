using Casamento.Domain.Rsvps.ValueObjects;

namespace Casamento.Domain.Gifts.ValueObjects;

public sealed record Purchase(
    string BuyerName,
    Email BuyerEmail,
    string AsaasPaymentId,
    DateTimeOffset PaidAt,
    /// <summary>
    /// Valor efetivamente cobrado nesta compra (o preço do presente no momento em que ela
    /// aconteceu). O preço do presente pode mudar depois; isto preserva o que cada um pagou.
    /// </summary>
    Money Amount,
    string? Message = null);
