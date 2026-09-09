using Casamento.Application.Abstractions;
using Casamento.Application.Common;
using Casamento.Infrastructure.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Casamento.Api.Infrastructure;

public interface IAdminAuthorization
{
    AuthPrincipal? Resolve(HttpRequest request);
    bool TryAuthorize(HttpRequest request, out AuthPrincipal principal);
}

internal sealed class AdminAuthorization(
    IAuthTokenService tokens,
    IClock clock,
    IOptions<AuthOptions> authOptions) : IAdminAuthorization
{
    private readonly AuthOptions _options = authOptions.Value;

    public AuthPrincipal? Resolve(HttpRequest request)
    {
        if (!request.Cookies.TryGetValue(_options.CookieName, out var cookieValue) || string.IsNullOrWhiteSpace(cookieValue))
        {
            if (request.Headers.TryGetValue("Authorization", out var header)
                && header.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                cookieValue = header.ToString()["Bearer ".Length..];
            }
            else
            {
                return null;
            }
        }

        return tokens.ValidateToken(cookieValue, clock.UtcNow);
    }

    public bool TryAuthorize(HttpRequest request, out AuthPrincipal principal)
    {
        var resolved = Resolve(request);
        principal = resolved!;
        return resolved is not null;
    }
}
