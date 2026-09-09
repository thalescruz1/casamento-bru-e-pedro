using Casamento.Application.Abstractions;
using Casamento.Application.Common;
using Casamento.Application.Gifts.Dtos;
using Casamento.Application.Gifts.Mapping;
using Casamento.Domain.Gifts;
using MediatR;

namespace Casamento.Application.Gifts.Queries.ListAvailableGifts;

public sealed record ListAvailableGiftsQuery : IRequest<Result<IReadOnlyList<GiftDto>>>;

public sealed class ListAvailableGiftsHandler(
    IGiftRepository repository,
    IGiftImageStorage storage)
    : IRequestHandler<ListAvailableGiftsQuery, Result<IReadOnlyList<GiftDto>>>
{
    public async Task<Result<IReadOnlyList<GiftDto>>> Handle(
        ListAvailableGiftsQuery request,
        CancellationToken cancellationToken)
    {
        var gifts = await repository.ListAsync(includeUnavailable: false, cancellationToken).ConfigureAwait(false);

        IReadOnlyList<GiftDto> dtos = gifts
            .Where(g => g.Status == GiftStatus.Available)
            .Select(g => GiftMapping.ToPublicDto(g, storage))
            .ToArray();

        return Result<IReadOnlyList<GiftDto>>.Success(dtos);
    }
}
