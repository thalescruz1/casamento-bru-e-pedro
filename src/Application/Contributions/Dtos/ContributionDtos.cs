using Casamento.Domain.Contributions;

namespace Casamento.Application.Contributions.Dtos;

public sealed record ContributionAdminDto(
    Guid Id,
    decimal Amount,
    string Currency,
    ContributionStatus Status,
    string? ContributorName,
    string? ContributorEmail,
    string? Message,
    string? AsaasPaymentId,
    DateTimeOffset? PaidAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CheckoutContributionPixResultDto(
    string PixKey,
    string PixKeyType,
    string Beneficiary,
    string? Bank,
    decimal Amount,
    string QrCodePayload,
    string QrCodeImageBase64);

public sealed record CheckoutContributionCardResultDto(
    Guid ContributionId,
    string AsaasPaymentId,
    string Status);

public sealed record ConfirmManualContributionPixResultDto(
    Guid ContributionId,
    string Status);
