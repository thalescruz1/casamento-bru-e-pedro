using Casamento.Api.Infrastructure;
using Casamento.Application.Auth.Commands.ChangePassword;
using Casamento.Application.Auth.Commands.Login;
using Casamento.Application.Auth.Dtos;
using Casamento.Application.Auth.Queries.GetCurrentUser;
using Casamento.Infrastructure.Auth;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Options;

namespace Casamento.Api.Functions;

public sealed class AuthFunctions(
    ISender sender,
    IClientIpHasher ipHasher,
    IAdminAuthorization authorization,
    IOptions<AuthOptions> authOptions)
{
    private readonly AuthOptions _options = authOptions.Value;

    [Function("Login")]
    public async Task<IActionResult> Login(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/login")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var body = await request.ReadFromJsonAsync<LoginBody>(cancellationToken).ConfigureAwait(false);
        if (body is null)
        {
            return new BadRequestObjectResult(new { error = "validation", message = "Corpo inválido." });
        }

        var command = new LoginCommand(body.Email ?? string.Empty, body.Password ?? string.Empty, ipHasher.Hash(request));
        var result = await sender.Send(command, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return result.ToActionResult();
        }

        SetSessionCookie(request, result.Value!);
        return new ObjectResult(new { user = result.Value!.User, expiresAt = result.Value.ExpiresAt })
        {
            StatusCode = StatusCodes.Status200OK
        };
    }

    [Function("Logout")]
    public IActionResult Logout(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/logout")] HttpRequest request)
    {
        ClearSessionCookie(request);
        return new NoContentResult();
    }

    [Function("Me")]
    public async Task<IActionResult> Me(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/me")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (!authorization.TryAuthorize(request, out var principal))
        {
            return new UnauthorizedResult();
        }

        var result = await sender.Send(new GetCurrentUserQuery(principal.UserId), cancellationToken).ConfigureAwait(false);
        return result.ToActionResult();
    }

    [Function("ChangePassword")]
    public async Task<IActionResult> ChangePassword(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/change-password")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (!authorization.TryAuthorize(request, out var principal))
        {
            return new UnauthorizedResult();
        }

        var body = await request.ReadFromJsonAsync<ChangePasswordBody>(cancellationToken).ConfigureAwait(false);
        if (body is null)
        {
            return new BadRequestObjectResult(new { error = "validation", message = "Corpo inválido." });
        }

        var command = new ChangePasswordCommand(principal.UserId, body.CurrentPassword ?? string.Empty, body.NewPassword ?? string.Empty);
        var result = await sender.Send(command, cancellationToken).ConfigureAwait(false);
        return result.ToActionResult();
    }

    private void SetSessionCookie(HttpRequest request, LoginResultDto result)
    {
        var secure = !string.Equals(request.Host.Host, "localhost", StringComparison.OrdinalIgnoreCase);
        var cookie = new Microsoft.AspNetCore.Http.CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            Expires = result.ExpiresAt
        };
        request.HttpContext.Response.Cookies.Append(_options.CookieName, result.Token, cookie);
    }

    private void ClearSessionCookie(HttpRequest request)
    {
        var secure = !string.Equals(request.Host.Host, "localhost", StringComparison.OrdinalIgnoreCase);
        request.HttpContext.Response.Cookies.Delete(_options.CookieName, new Microsoft.AspNetCore.Http.CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSiteMode.Strict,
            Path = "/"
        });
    }

    private sealed record LoginBody(string? Email, string? Password);
    private sealed record ChangePasswordBody(string? CurrentPassword, string? NewPassword);
}
