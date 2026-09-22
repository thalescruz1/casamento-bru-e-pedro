using Casamento.Application.Abstractions;
using Casamento.Application.Gifts.Dtos;
using Casamento.Domain.Gifts;

namespace Casamento.Application.Gifts.Mapping;

internal static class GiftMapping
{
    public static readonly TimeSpan SasLifetime = TimeSpan.FromHours(6);

    public static GiftDto ToPublicDto(Gift gift, IGiftImageStorage storage)
    {
        var url = gift.ImageBlobName is null ? null : storage.BuildPublicUrl(gift.ImageBlobName, SasLifetime);
        return new GiftDto(gift.Id, gift.Title, gift.Description, url, gift.Price.Amount, gift.Price.Currency, gift.Status);
    }

    public static GiftAdminDto ToAdminDto(Gift gift, IGiftImageStorage storage)
    {
        var url = gift.ImageBlobName is null ? null : storage.BuildPublicUrl(gift.ImageBlobName, SasLifetime);
        return new GiftAdminDto(
            gift.Id,
            gift.Title,
            gift.Description,
            url,
            gift.ImageBlobName,
            gift.Price.Amount,
            gift.Price.Currency,
            gift.Status,
            gift.MaxPurchases,
            gift.PurchaseCount,
            gift.Purchases
                .Select(p => new GiftPurchaseDto(p.BuyerName, p.BuyerEmail.Value, p.Message, p.AsaasPaymentId, p.PaidAt, p.Amount.Amount, p.Amount.Currency))
                .ToArray(),
            gift.CreatedAt,
            gift.UpdatedAt);
    }
}
