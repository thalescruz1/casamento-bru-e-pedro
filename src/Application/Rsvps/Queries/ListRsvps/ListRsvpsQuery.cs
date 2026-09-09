using Casamento.Application.Abstractions;
using Casamento.Application.Common;
using Casamento.Application.Rsvps.Dtos;
using MediatR;

namespace Casamento.Application.Rsvps.Queries.ListRsvps;

public sealed record ListRsvpsQuery(int Skip, int Take) : IRequest<Result<RsvpListDto>>;

public sealed class ListRsvpsHandler(IRsvpRepository repository)
    : IRequestHandler<ListRsvpsQuery, Result<RsvpListDto>>
{
    public async Task<Result<RsvpListDto>> Handle(ListRsvpsQuery request, CancellationToken cancellationToken)
    {
        var skip = Math.Max(0, request.Skip);
        var take = Math.Clamp(request.Take <= 0 ? 50 : request.Take, 1, 200);

        var items = await repository.ListAsync(skip, take, cancellationToken).ConfigureAwait(false);
        var total = await repository.CountAsync(cancellationToken).ConfigureAwait(false);

        var dtos = items
            .Select(r => new RsvpListItemDto(
                r.Id,
                r.Name,
                r.Email.Value,
                r.Phone,
                r.Attend,
                r.Guests,
                r.GuestNames,
                r.Restrictions,
                r.SubmittedAt))
            .ToArray();

        return new RsvpListDto(dtos, total);
    }
}
