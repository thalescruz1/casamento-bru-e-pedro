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

    /// <summary>Docs antigos não têm o campo: o padrão 1 preserva o comportamento de compra única.</summary>
    [JsonProperty("maxPurchases")]
    public int MaxPurchases { get; set; } = 1;

    [JsonProperty("unlimited")]
    public bool Unlimited { get; set; }

    [JsonProperty("purchases")]
    public List<PurchaseDocument>? Purchases { get; set; }

    /// <summary>Exclusão lógica: o presente some das listas, mas o documento (e as compras) fica.</summary>
    [JsonProperty("deletedAt")]
    public DateTimeOffset? DeletedAt { get; set; }

    // Campos legados (compra única). Só são lidos; novos registros usam "purchases".
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

    // Preenchido pelo Cosmos na leitura; não é enviado na gravação.
    [JsonProperty("_etag", NullValueHandling = NullValueHandling.Ignore)]
    public string? ETag { get; set; }

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
        MaxPurchases = gift.MaxPurchases ?? 1,
        Unlimited = gift.MaxPurchases is null,
        Purchases = gift.Purchases.Select(PurchaseDocument.FromPurchase).ToList(),
        DeletedAt = gift.DeletedAt,
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

        SetPrivate(gift, nameof(Gift.MaxPurchases), Unlimited ? null : (int?)MaxPurchases);
        SetPrivate(gift, nameof(Gift.DeletedAt), DeletedAt);

        var purchases = Purchases is { Count: > 0 }
            ? Purchases.Select(p => p.ToPurchase()).ToList()
            : ReadLegacyPurchase(effectiveStatus);
        var field = typeof(Gift).GetField("_purchases", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        ((List<Purchase>)field.GetValue(gift)!).AddRange(purchases);

        SetPrivate(gift, nameof(Gift.CreatedAt), CreatedAt);
        SetPrivate(gift, nameof(Gift.UpdatedAt), UpdatedAt);

        return gift;
    }

    private List<Purchase> ReadLegacyPurchase(GiftStatus effectiveStatus)
    {
        if (effectiveStatus == GiftStatus.Paid
            && BuyerName is not null
            && BuyerEmail is not null
            && AsaasPaymentId is not null
            && PaidAt.HasValue)
        {
            return
            [
                new Purchase(
                    BuyerName: BuyerName,
                    BuyerEmail: Email.Create(BuyerEmail),
                    AsaasPaymentId: AsaasPaymentId,
                    PaidAt: PaidAt.Value,
                    // Doc antigo (pré multi-compra) não guardava o valor por compra à parte;
                    // o preço do presente na época é a melhor aproximação disponível.
                    Amount: Money.FromBrl(PriceAmount),
                    Message: BuyerMessage)
            ];
        }

        return [];
    }

    private static void SetPrivate(object target, string propertyName, object? value)
    {
        var prop = target.GetType().GetProperty(propertyName)!;
        prop.GetSetMethod(nonPublic: true)!.Invoke(target, [value]);
    }
}

internal sealed class PurchaseDocument
{
    [JsonProperty("buyerName")]
    public string BuyerName { get; set; } = string.Empty;

    [JsonProperty("buyerEmail")]
    public string BuyerEmail { get; set; } = string.Empty;

    [JsonProperty("message")]
    public string? Message { get; set; }

    [JsonProperty("asaasPaymentId")]
    public string AsaasPaymentId { get; set; } = string.Empty;

    [JsonProperty("paidAt")]
    public DateTimeOffset PaidAt { get; set; }

    [JsonProperty("amount")]
    public decimal Amount { get; set; }

    [JsonProperty("currency")]
    public string Currency { get; set; } = "BRL";

    public static PurchaseDocument FromPurchase(Purchase purchase) => new()
    {
        BuyerName = purchase.BuyerName,
        BuyerEmail = purchase.BuyerEmail.Value,
        Message = purchase.Message,
        AsaasPaymentId = purchase.AsaasPaymentId,
        PaidAt = purchase.PaidAt,
        Amount = purchase.Amount.Amount,
        Currency = purchase.Amount.Currency
    };

    public Purchase ToPurchase() => new(
        BuyerName: BuyerName,
        BuyerEmail: Email.Create(BuyerEmail),
        AsaasPaymentId: AsaasPaymentId,
        PaidAt: PaidAt,
        Amount: Money.FromBrl(Amount),
        Message: Message);
}
