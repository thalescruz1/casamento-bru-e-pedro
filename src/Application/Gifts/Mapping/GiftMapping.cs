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
            gift.Purchase?.BuyerName,
            gift.Purchase?.BuyerEmail.Value,
            gift.Purchase?.Message,
            gift.Purchase?.AsaasPaymentId,
            gift.Purchase?.PaidAt,
            gift.CreatedAt,
            gift.UpdatedAt);
    }
}
