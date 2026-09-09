using Casamento.Domain.Common;
using Casamento.Domain.Contributions.Events;
using Casamento.Domain.Gifts.ValueObjects;
using Casamento.Domain.Rsvps.ValueObjects;

namespace Casamento.Domain.Contributions;

public sealed class Contribution : AggregateRoot
{
    public const int MaxMessageLength = 600;
    public const decimal MinAmount = 5m;

    public Guid Id { get; private set; }
    public Money Amount { get; private set; }
    public ContributionStatus Status { get; private set; }
    public string? ContributorName { get; private set; }
    public Email? ContributorEmail { get; private set; }
    public string? Message { get; private set; }
    public string? AsaasPaymentId { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Contribution() { }

    public static Contribution Create(Money amount, DateTimeOffset now)
    {
        if (amount.Amount < MinAmount)
        {
            throw new DomainException($"Valor mínimo de contribuição é R$ {MinAmount:0.00}.");
        }

        var contribution = new Contribution
        {
            Id = GuidV7.NewGuid(),
            Amount = amount,
            Status = ContributionStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };

        contribution.Raise(new ContributionCreated(contribution.Id, amount.Amount, now));
        return contribution;
    }

    /// <summary>
    /// Marca contribuição como paga. Primeiro pagamento vence; segundo emite
    /// <see cref="ContributionPaymentConflict"/> sem alterar estado.
    /// </summary>
    public void MarkPaid(
        string contributorName,
        Email contributorEmail,
        string asaasPaymentId,
        DateTimeOffset now,
        string? message = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contributorName);
        ArgumentException.ThrowIfNullOrWhiteSpace(asaasPaymentId);

        var trimmedName = contributorName.Trim();
        if (trimmedName.Length is < 2 or > 120)
        {
            throw new DomainException("Nome do contribuidor inválido.");
        }

        var sanitizedMessage = string.IsNullOrWhiteSpace(message) ? null : message.Trim();
        if (sanitizedMessage is { Length: > MaxMessageLength })
        {
            throw new DomainException($"Mensagem excede {MaxMessageLength} caracteres.");
        }

        if (Status == ContributionStatus.Paid)
        {
            if (AsaasPaymentId == asaasPaymentId)
            {
                return;
            }

            Raise(new ContributionPaymentConflict(Id, asaasPaymentId, AsaasPaymentId ?? string.Empty, now));
            return;
        }

        if (Status != ContributionStatus.Pending)
        {
            throw new DomainException("Contribuição não pode mais ser paga.");
        }

        ContributorName = trimmedName;
        ContributorEmail = contributorEmail;
        Message = sanitizedMessage;
        AsaasPaymentId = asaasPaymentId;
        PaidAt = now;
        Status = ContributionStatus.Paid;
        UpdatedAt = now;

        Raise(new ContributionPaid(Id, trimmedName, contributorEmail.Value, asaasPaymentId, now));
    }

    public void Cancel(DateTimeOffset now)
    {
        if (Status == ContributionStatus.Refused)
        {
            return;
        }

        if (Status == ContributionStatus.Paid)
        {
            throw new DomainException("Não é possível cancelar uma contribuição já paga.");
        }

        Status = ContributionStatus.Refused;
        UpdatedAt = now;

        Raise(new ContributionCanceled(Id, now));
    }
}
