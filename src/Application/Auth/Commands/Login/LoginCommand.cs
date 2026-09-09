using Casamento.Application.Abstractions;
using Casamento.Application.Auth.Dtos;
using Casamento.Application.Common;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Casamento.Application.Auth.Commands.Login;

public sealed record LoginCommand(string Email, string Password, string IpHash) : IRequest<Result<LoginResultDto>>;

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(200);
        RuleFor(x => x.IpHash).NotEmpty();
    }
}

public sealed class LoginHandler(
    IAdminUserRepository repository,
    IPasswordHasher hasher,
    IAuthTokenService tokens,
    IClock clock,
    ILogger<LoginHandler> logger) : IRequestHandler<LoginCommand, Result<LoginResultDto>>
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public async Task<Result<LoginResultDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var user = await repository.FindByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            logger.LogWarning("Tentativa de login com e-mail desconhecido (IP {IpHashPrefix})", request.IpHash[..Math.Min(8, request.IpHash.Length)]);
            return Error.Validation("E-mail ou senha inválidos.");
        }

        if (user.IsLocked(now))
        {
            return Error.RateLimited("Conta temporariamente bloqueada. Tente novamente em alguns minutos.");
        }

        if (!hasher.Verify(user.PasswordHash, request.Password))
        {
            user.RegisterFailedLogin(now, MaxFailedAttempts, LockoutDuration);
            await repository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
            logger.LogWarning("Login falhou para {UserId} ({Attempts}/{Max})", user.Id, user.FailedAttempts, MaxFailedAttempts);
            return Error.Validation("E-mail ou senha inválidos.");
        }

        user.RegisterSuccessfulLogin(now);
        await repository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);

        var principal = new AuthPrincipal(user.Id, user.Email.Value, user.DisplayName, now + tokens.SessionLifetime);
        var token = tokens.IssueToken(principal);

        logger.LogInformation("Admin {UserId} autenticado", user.Id);
        return new LoginResultDto(token.Token, token.ExpiresAt, new CurrentUserDto(user.Id, user.Email.Value, user.DisplayName));
    }
}
