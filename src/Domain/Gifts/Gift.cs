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

    public Guid Id { get; private set; }
    public string Title { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public string? ImageBlobName { get; private set; }
    public Money Price { get; private set; }
    public GiftStatus Status { get; private set; }
    public Purchase? Purchase { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Gift() { }

    public static Gift Create(
        string title,
        string description,
        Money price,
        string? imageBlobName,
        DateTimeOffset now)
    {
        var (validatedTitle, validatedDescription) = ValidateTextFields(title, description);

        var gift = new Gift
        {
            Id = GuidV7.NewGuid(),
            Title = validatedTitle,
            Description = validatedDescription,
            Price = price,
            ImageBlobName = imageBlobName,
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
        DateTimeOffset now)
    {
        if (Status != GiftStatus.Available)
        {
            throw new DomainException("Só é possível editar um presente disponível.");
        }

        var (validatedTitle, validatedDescription) = ValidateTextFields(title, description);

        Title = validatedTitle;
        Description = validatedDescription;
        Price = price;
        ImageBlobName = imageBlobName;
        UpdatedAt = now;
    }

    /// <summary>
    /// Marca o presente como pago. Primeiro pagamento a chegar vence; segundo emite
    /// <see cref="GiftPaymentConflict"/> para estorno manual sem alterar estado.
    /// </summary>
    public const int MaxMessageLength = 600;

    public void MarkPaid(
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

        if (Status == GiftStatus.Paid)
        {
            if (Purchase?.AsaasPaymentId == asaasPaymentId)
            {
                return;
            }

            Raise(new GiftPaymentConflict(Id, asaasPaymentId, Purchase?.AsaasPaymentId ?? string.Empty, now));
            return;
        }

        if (Status != GiftStatus.Available)
        {
            throw new DomainException("Este presente não está mais disponível.");
        }

        Purchase = new Purchase(trimmed, buyerEmail, asaasPaymentId, now, sanitizedMessage);
        Status = GiftStatus.Paid;
        UpdatedAt = now;

        Raise(new GiftPaid(Id, trimmed, buyerEmail.Value, asaasPaymentId, now));
    }

    public void Cancel(DateTimeOffset now)
    {
        if (Status == GiftStatus.Canceled)
        {
            return;
        }

        if (Status == GiftStatus.Paid)
        {
            throw new DomainException("Não é possível cancelar um presente já pago.");
        }

        Status = GiftStatus.Canceled;
        UpdatedAt = now;

        Raise(new GiftCanceled(Id, now));
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
