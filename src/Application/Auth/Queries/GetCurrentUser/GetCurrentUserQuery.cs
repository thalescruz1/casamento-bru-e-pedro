using Casamento.Application.Abstractions;
using Casamento.Application.Auth.Dtos;
using Casamento.Application.Common;
using MediatR;

namespace Casamento.Application.Auth.Queries.GetCurrentUser;

public sealed record GetCurrentUserQuery(Guid UserId) : IRequest<Result<CurrentUserDto>>;

public sealed class GetCurrentUserHandler(IAdminUserRepository repository)
    : IRequestHandler<GetCurrentUserQuery, Result<CurrentUserDto>>
{
    public async Task<Result<CurrentUserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await repository.FindByIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return Error.NotFound("Usuário não encontrado.");
        }

        return new CurrentUserDto(user.Id, user.Email.Value, user.DisplayName);
    }
}
