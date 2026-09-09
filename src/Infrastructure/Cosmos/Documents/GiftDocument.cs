using Casamento.Domain.Gifts;
using Casamento.Domain.Gifts.ValueObjects;
using Casamento.Domain.Rsvps.ValueObjects;
using Newtonsoft.Json;

namespace Casamento.Infrastructure.Cosmos.Documents;

internal sealed class GiftDocument
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("type")]
    public string Type { get; set; } = "gift";

    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;

    [JsonProperty("imageBlobName")]
    public string? ImageBlobName { get; set; }

    [JsonProperty("priceAmount")]
    public decimal PriceAmount { get; set; }

    [JsonProperty("priceCurrency")]
    public string PriceCurrency { get; set; } = "BRL";

    [JsonProperty("status")]
    public GiftStatus Status { get; set; }

    [JsonProperty("buyerName")]
    public string? BuyerName { get; set; }

    [JsonProperty("buyerEmail")]
    public string? BuyerEmail { get; set; }

    [JsonProperty("buyerMessage")]
    public string? BuyerMessage { get; set; }

    [JsonProperty("asaasPaymentId")]
    public string? AsaasPaymentId { get; set; }

    [JsonProperty("paidAt")]
    public DateTimeOffset? PaidAt { get; set; }

    [JsonProperty("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }

    [JsonProperty("updatedAt")]
    public DateTimeOffset UpdatedAt { get; set; }

    public static GiftDocument FromAggregate(Gift gift) => new()
    {
        Id = gift.Id.ToString("N"),
        Type = "gift",
        Title = gift.Title,
        Description = gift.Description,
        ImageBlobName = gift.ImageBlobName,
        PriceAmount = gift.Price.Amount,
        PriceCurrency = gift.Price.Currency,
        Status = gift.Status,
        BuyerName = gift.Purchase?.BuyerName,
        BuyerEmail = gift.Purchase?.BuyerEmail.Value,
        BuyerMessage = gift.Purchase?.Message,
        AsaasPaymentId = gift.Purchase?.AsaasPaymentId,
        PaidAt = gift.Purchase?.PaidAt,
        CreatedAt = gift.CreatedAt,
        UpdatedAt = gift.UpdatedAt
    };

    public Gift ToAggregate()
    {
        var ctor = typeof(Gift).GetConstructor(
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            System.Type.EmptyTypes)!;
        var gift = (Gift)ctor.Invoke(null);

        SetPrivate(gift, nameof(Gift.Id), Guid.ParseExact(Id, "N"));
        SetPrivate(gift, nameof(Gift.Title), Title);
        SetPrivate(gift, nameof(Gift.Description), Description);
        SetPrivate(gift, nameof(Gift.ImageBlobName), ImageBlobName);
        SetPrivate(gift, nameof(Gift.Price), Money.FromBrl(PriceAmount));

        // Legacy: docs antigos podiam ter status=Reserved (2). Agora esse estado não existe
        // e ele é mapeado de volta para Available (o comprador que estava "reservando" pagou
        // de verdade via o novo fluxo ou simplesmente não levou o item).
        var effectiveStatus = Status == (GiftStatus)2 ? GiftStatus.Available : Status;
        SetPrivate(gift, nameof(Gift.Status), effectiveStatus);

        if (effectiveStatus == GiftStatus.Paid
            && BuyerName is not null
            && BuyerEmail is not null
            && AsaasPaymentId is not null
            && PaidAt.HasValue)
        {
            var purchase = new Purchase(
                BuyerName: BuyerName,
                BuyerEmail: Email.Create(BuyerEmail),
                AsaasPaymentId: AsaasPaymentId,
                PaidAt: PaidAt.Value,
                Message: BuyerMessage);
            SetPrivate(gift, nameof(Gift.Purchase), purchase);
        }

        SetPrivate(gift, nameof(Gift.CreatedAt), CreatedAt);
        SetPrivate(gift, nameof(Gift.UpdatedAt), UpdatedAt);

        return gift;
    }

    private static void SetPrivate(object target, string propertyName, object? value)
    {
        var prop = target.GetType().GetProperty(propertyName)!;
        prop.GetSetMethod(nonPublic: true)!.Invoke(target, [value]);
    }
}
