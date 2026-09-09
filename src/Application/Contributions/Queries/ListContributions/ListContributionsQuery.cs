using Casamento.Application.Abstractions;
using Casamento.Application.Common;
using Casamento.Application.Contributions.Dtos;
using Casamento.Application.Contributions.Mapping;
using MediatR;

namespace Casamento.Application.Contributions.Queries.ListContributions;

public sealed record ListContributionsQuery : IRequest<Result<IReadOnlyList<ContributionAdminDto>>>;

public sealed class ListContributionsHandler(IContributionRepository repository)
    : IRequestHandler<ListContributionsQuery, Result<IReadOnlyList<ContributionAdminDto>>>
{
    public async Task<Result<IReadOnlyList<ContributionAdminDto>>> Handle(
        ListContributionsQuery request,
        CancellationToken cancellationToken)
    {
        var contributions = await repository.ListAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<ContributionAdminDto> dtos = contributions
            .Select(ContributionMapping.ToAdminDto)
            .ToArray();
        return Result<IReadOnlyList<ContributionAdminDto>>.Success(dtos);
    }
}
