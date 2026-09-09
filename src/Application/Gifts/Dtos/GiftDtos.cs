using Casamento.Domain.Gifts;

namespace Casamento.Application.Gifts.Dtos;

public sealed record GiftDto(
    Guid Id,
    string Title,
    string Description,
    string? ImageUrl,
    decimal Price,
    string Currency,
    GiftStatus Status);

public sealed record GiftAdminDto(
    Guid Id,
    string Title,
    string Description,
    string? ImageUrl,
    string? ImageBlobName,
    decimal Price,
    string Currency,
    GiftStatus Status,
    string? BuyerName,
    string? BuyerEmail,
    string? BuyerMessage,
    string? AsaasPaymentId,
    DateTimeOffset? PaidAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CheckoutPixResultDto(
    Guid GiftId,
    string PixKey,
    string PixKeyType,
    string Beneficiary,
    string? Bank,
    decimal Amount,
    string QrCodePayload,
    string QrCodeImageBase64);

public sealed record CheckoutCardResultDto(
    Guid GiftId,
    string AsaasPaymentId,
    string Status);

public sealed record UploadedImageDto(string BlobName, string Url);
