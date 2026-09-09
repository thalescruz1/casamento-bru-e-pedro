using Casamento.Application.Abstractions;
using Casamento.Application.Common;
using Casamento.Application.Gifts.Dtos;
using Casamento.Application.Gifts.Mapping;
using MediatR;

namespace Casamento.Application.Gifts.Queries.ListAllGifts;

public sealed record ListAllGiftsQuery : IRequest<Result<IReadOnlyList<GiftAdminDto>>>;

public sealed class ListAllGiftsHandler(
    IGiftRepository repository,
    IGiftImageStorage storage)
    : IRequestHandler<ListAllGiftsQuery, Result<IReadOnlyList<GiftAdminDto>>>
{
    public async Task<Result<IReadOnlyList<GiftAdminDto>>> Handle(
        ListAllGiftsQuery request,
        CancellationToken cancellationToken)
    {
        var gifts = await repository.ListAsync(includeUnavailable: true, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<GiftAdminDto> dtos = gifts
            .Select(g => GiftMapping.ToAdminDto(g, storage))
            .ToArray();
        return Result<IReadOnlyList<GiftAdminDto>>.Success(dtos);
    }
}
