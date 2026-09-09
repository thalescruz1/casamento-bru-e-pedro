namespace Casamento.Application.Received.Dtos;

public sealed record ReceivedItemDto(
    string Type,
    Guid Id,
    string Title,
    decimal Amount,
    string Currency,
    string? ContributorName,
    string? ContributorEmail,
    string? Message,
    string? AsaasPaymentId,
    string PaymentMethod,
    DateTimeOffset PaidAt);
