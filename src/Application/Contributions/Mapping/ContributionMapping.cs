using Casamento.Application.Contributions.Dtos;
using Casamento.Domain.Contributions;

namespace Casamento.Application.Contributions.Mapping;

internal static class ContributionMapping
{
    public static ContributionAdminDto ToAdminDto(Contribution contribution)
    {
        return new ContributionAdminDto(
            contribution.Id,
            contribution.Amount.Amount,
            contribution.Amount.Currency,
            contribution.Status,
            contribution.ContributorName,
            contribution.ContributorEmail?.Value,
            contribution.Message,
            contribution.AsaasPaymentId,
            contribution.PaidAt,
            contribution.CreatedAt,
            contribution.UpdatedAt);
    }
}
