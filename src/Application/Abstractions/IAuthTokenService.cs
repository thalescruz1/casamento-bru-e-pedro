namespace Casamento.Application.Abstractions;

public sealed record AuthPrincipal(Guid UserId, string Email, string DisplayName, DateTimeOffset ExpiresAt);

public sealed record AuthTokenResult(string Token, DateTimeOffset ExpiresAt);

public interface IAuthTokenService
{
    AuthTokenResult IssueToken(AuthPrincipal principal);
    AuthPrincipal? ValidateToken(string? token, DateTimeOffset now);
    TimeSpan SessionLifetime { get; }
}
