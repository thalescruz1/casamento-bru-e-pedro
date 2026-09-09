using Casamento.Domain.Common;

namespace Casamento.Domain.Gifts.Events;

public sealed record GiftCreated(Guid GiftId, string Title, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record GiftPaid(
    Guid GiftId,
    string BuyerName,
    string BuyerEmail,
    string AsaasPaymentId,
    DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record GiftPaymentConflict(
    Guid GiftId,
    string AttemptedPaymentId,
    string WinningPaymentId,
    DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record GiftCanceled(Guid GiftId, DateTimeOffset OccurredAt) : IDomainEvent;
