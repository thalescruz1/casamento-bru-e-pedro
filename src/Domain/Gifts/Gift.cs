using Casamento.Domain.Common;
using Casamento.Domain.Gifts.Events;
using Casamento.Domain.Gifts.ValueObjects;
using Casamento.Domain.Rsvps.ValueObjects;

namespace Casamento.Domain.Gifts;

public sealed class Gift : AggregateRoot
{
    public const int MinTitleLength = 3;
    public const int MaxTitleLength = 120;
    public const int MaxDescriptionLength = 500;
    public const int MaxPurchasesLimit = 1000;

    private readonly List<Purchase> _purchases = [];

    public Guid Id { get; private set; }
    public string Title { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public string? ImageBlobName { get; private set; }
    public Money Price { get; private set; }
    public GiftStatus Status { get; private set; }

    /// <summary>Quantas vezes o presente pode ser comprado. <c>null</c> = ilimitado.</summary>
    public int? MaxPurchases { get; private set; } = 1;

    public IReadOnlyList<Purchase> Purchases => _purchases;

    /// <summary>Compra mais recente (a que acabou de ser registrada, no fluxo de pagamento).</summary>
    public Purchase? Purchase => _purchases.Count == 0 ? null : _purchases[^1];

    public int PurchaseCount => _purchases.Count;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Gift() { }

    public static Gift Create(
        string title,
        string description,
        Money price,
        string? imageBlobName,
        DateTimeOffset now,
        int? maxPurchases = 1)
    {
        var (validatedTitle, validatedDescription) = ValidateTextFields(title, description);
        ValidateMaxPurchases(maxPurchases, purchaseCount: 0);

        var gift = new Gift
        {
            Id = GuidV7.NewGuid(),
            Title = validatedTitle,
            Description = validatedDescription,
            Price = price,
            ImageBlobName = imageBlobName,
            MaxPurchases = maxPurchases,
            Status = GiftStatus.Available,
            CreatedAt = now,
            UpdatedAt = now
        };

        gift.Raise(new GiftCreated(gift.Id, gift.Title, now));
        return gift;
    }

    public void UpdateDetails(
        string title,
        string description,
        Money price,
        string? imageBlobName,
        int? maxPurchases,
        DateTimeOffset now)
    {
        if (Status == GiftStatus.Canceled)
        {
            throw new DomainException("Este presente foi cancelado e não pode ser editado.");
        }

        if (_purchases.Count > 0 && price != Price)
        {
            throw new DomainException("O valor não pode mudar depois que o presente já foi comprado.");
        }

        var (validatedTitle, validatedDescription) = ValidateTextFields(title, description);
        ValidateMaxPurchases(maxPurchases, _purchases.Count);

        Title = validatedTitle;
        Description = validatedDescription;
        Price = price;
        ImageBlobName = imageBlobName;
        MaxPurchases = maxPurchases;
        Status = IsSoldOut() ? GiftStatus.Paid : GiftStatus.Available;
        UpdatedAt = now;
    }

    public const int MaxMessageLength = 600;

    /// <summary>
    /// Registra uma compra. Devolve <c>true</c> quando uma compra nova foi registrada.
    /// Se o mesmo pagamento já estava registrado, é um no-op idempotente (<c>false</c>). Se o presente
    /// já esgotou (limite atingido), emite <see cref="GiftPaymentConflict"/> para estorno manual sem
    /// alterar estado (<c>false</c>). Ao atingir o limite de compras, o presente passa a <see cref="GiftStatus.Paid"/>.
    /// </summary>
    public bool MarkPaid(
        string buyerName,
        Email buyerEmail,
        string asaasPaymentId,
        DateTimeOffset now,
        string? message = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(buyerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(asaasPaymentId);

        var trimmed = buyerName.Trim();
        if (trimmed.Length is < 2 or > 120)
        {
            throw new DomainException("Nome do comprador inválido.");
        }

        var sanitizedMessage = string.IsNullOrWhiteSpace(message) ? null : message.Trim();
        if (sanitizedMessage is { Length: > MaxMessageLength })
        {
            throw new DomainException($"Mensagem excede {MaxMessageLength} caracteres.");
        }

        if (HasPayment(asaasPaymentId))
        {
            return false;
        }

        if (Status == GiftStatus.Paid)
        {
            Raise(new GiftPaymentConflict(Id, asaasPaymentId, Purchase?.AsaasPaymentId ?? string.Empty, now));
            return false;
        }

        if (Status != GiftStatus.Available)
        {
            throw new DomainException("Este presente não está mais disponível.");
        }

        _purchases.Add(new Purchase(trimmed, buyerEmail, asaasPaymentId, now, sanitizedMessage));
        if (IsSoldOut())
        {
            Status = GiftStatus.Paid;
        }

        UpdatedAt = now;

        Raise(new GiftPaid(Id, trimmed, buyerEmail.Value, asaasPaymentId, now));
        return true;
    }

    public bool HasPayment(string asaasPaymentId) =>
        _purchases.Exists(p => p.AsaasPaymentId == asaasPaymentId);

    public void Cancel(DateTimeOffset now)
    {
        if (Status == GiftStatus.Canceled)
        {
            return;
        }

        if (Status == GiftStatus.Paid || _purchases.Count > 0)
        {
            throw new DomainException("Não é possível cancelar um presente já pago.");
        }

        Status = GiftStatus.Canceled;
        UpdatedAt = now;

        Raise(new GiftCanceled(Id, now));
    }

    private bool IsSoldOut() => MaxPurchases is int max && _purchases.Count >= max;

    private static void ValidateMaxPurchases(int? maxPurchases, int purchaseCount)
    {
        if (maxPurchases is null)
        {
            return;
        }

        if (maxPurchases is < 1 or > MaxPurchasesLimit)
        {
            throw new DomainException($"O limite de compras deve ficar entre 1 e {MaxPurchasesLimit}, ou ser ilimitado.");
        }

        if (maxPurchases < purchaseCount)
        {
            throw new DomainException($"O limite não pode ser menor que as {purchaseCount} compras já feitas.");
        }
    }

    private static (string Title, string Description) ValidateTextFields(string title, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        var t = title.Trim();
        if (t.Length is < MinTitleLength or > MaxTitleLength)
        {
            throw new DomainException($"Título deve ter entre {MinTitleLength} e {MaxTitleLength} caracteres.");
        }

        var d = description.Trim();
        if (d.Length > MaxDescriptionLength)
        {
            throw new DomainException($"Descrição excede {MaxDescriptionLength} caracteres.");
        }

        return (t, d);
    }
}
