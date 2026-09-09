using Casamento.Domain.Contributions;
using Casamento.Domain.Gifts.ValueObjects;
using Casamento.Domain.Rsvps.ValueObjects;
using Newtonsoft.Json;

namespace Casamento.Infrastructure.Cosmos.Documents;

internal sealed class ContributionDocument
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("type")]
    public string Type { get; set; } = "contribution";

    [JsonProperty("amount")]
    public decimal Amount { get; set; }

    [JsonProperty("currency")]
    public string Currency { get; set; } = "BRL";

    [JsonProperty("status")]
    public ContributionStatus Status { get; set; }

    [JsonProperty("contributorName")]
    public string? ContributorName { get; set; }

    [JsonProperty("contributorEmail")]
    public string? ContributorEmail { get; set; }

    [JsonProperty("contributorMessage")]
    public string? ContributorMessage { get; set; }

    [JsonProperty("asaasPaymentId")]
    public string? AsaasPaymentId { get; set; }

    [JsonProperty("paidAt")]
    public DateTimeOffset? PaidAt { get; set; }

    [JsonProperty("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }

    [JsonProperty("updatedAt")]
    public DateTimeOffset UpdatedAt { get; set; }

    public static ContributionDocument FromAggregate(Contribution contribution) => new()
    {
        Id = contribution.Id.ToString("N"),
        Type = "contribution",
        Amount = contribution.Amount.Amount,
        Currency = contribution.Amount.Currency,
        Status = contribution.Status,
        ContributorName = contribution.ContributorName,
        ContributorEmail = contribution.ContributorEmail?.Value,
        ContributorMessage = contribution.Message,
        AsaasPaymentId = contribution.AsaasPaymentId,
        PaidAt = contribution.PaidAt,
        CreatedAt = contribution.CreatedAt,
        UpdatedAt = contribution.UpdatedAt
    };

    public Contribution ToAggregate()
    {
        var ctor = typeof(Contribution).GetConstructor(
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            System.Type.EmptyTypes)!;
        var contribution = (Contribution)ctor.Invoke(null);

        SetPrivate(contribution, nameof(Contribution.Id), Guid.ParseExact(Id, "N"));
        SetPrivate(contribution, nameof(Contribution.Amount), Money.FromBrl(Amount));
        SetPrivate(contribution, nameof(Contribution.Status), Status);
        SetPrivate(contribution, nameof(Contribution.ContributorName), ContributorName);
        if (!string.IsNullOrWhiteSpace(ContributorEmail))
        {
            SetPrivate(contribution, nameof(Contribution.ContributorEmail), Email.Create(ContributorEmail));
        }
        SetPrivate(contribution, nameof(Contribution.Message), ContributorMessage);
        SetPrivate(contribution, nameof(Contribution.AsaasPaymentId), AsaasPaymentId);
        SetPrivate(contribution, nameof(Contribution.PaidAt), PaidAt);
        SetPrivate(contribution, nameof(Contribution.CreatedAt), CreatedAt);
        SetPrivate(contribution, nameof(Contribution.UpdatedAt), UpdatedAt);

        return contribution;
    }

    private static void SetPrivate(object target, string propertyName, object? value)
    {
        var prop = target.GetType().GetProperty(propertyName)!;
        prop.GetSetMethod(nonPublic: true)!.Invoke(target, [value]);
    }
}
