using Casamento.Domain.Common;

namespace Casamento.Domain.Contributions.Events;

public sealed record ContributionCreated(
    Guid ContributionId,
    decimal Amount,
    DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record ContributionPaid(
    Guid ContributionId,
    string ContributorName,
    string ContributorEmail,
    string AsaasPaymentId,
    DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record ContributionPaymentConflict(
    Guid ContributionId,
    string AttemptedPaymentId,
    string WinningPaymentId,
    DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record ContributionCanceled(
    Guid ContributionId,
    DateTimeOffset OccurredAt) : IDomainEvent;
