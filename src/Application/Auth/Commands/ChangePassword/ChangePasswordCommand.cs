using Casamento.Application.Abstractions;
using Casamento.Application.Common;
using FluentValidation;
using MediatR;

namespace Casamento.Application.Auth.Commands.ChangePassword;

public sealed record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword) : IRequest<Result>;

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(10).MaximumLength(200);
    }
}

public sealed class ChangePasswordHandler(
    IAdminUserRepository repository,
    IPasswordHasher hasher,
    IClock clock) : IRequestHandler<ChangePasswordCommand, Result>
{
    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await repository.FindByIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return Error.NotFound("Usuário não encontrado.");
        }

        if (!hasher.Verify(user.PasswordHash, request.CurrentPassword))
        {
            return Error.Validation("Senha atual incorreta.");
        }

        user.ChangePassword(hasher.Hash(request.NewPassword), clock.UtcNow);
        await repository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
