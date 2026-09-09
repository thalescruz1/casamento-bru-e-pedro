using Casamento.Application.Abstractions;
using Casamento.Infrastructure.Auth;
using Microsoft.Extensions.Options;

namespace Casamento.Infrastructure.Tests.Auth;

public sealed class HmacAuthTokenServiceTests
{
    private static HmacAuthTokenService CreateService(string? key = null) =>
        new(Options.Create(new AuthOptions
        {
            TokenSigningKey = key ?? "0123456789abcdef0123456789abcdef",
            SessionLifetimeHours = 12
        }));

    [Fact]
    public void Issue_and_validate_roundtrip()
    {
        var svc = CreateService();
        var now = new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero);
        var principal = new AuthPrincipal(Guid.NewGuid(), "user@example.com", "User", now.AddHours(12));

        var token = svc.IssueToken(principal);
        var validated = svc.ValidateToken(token.Token, now);

        validated.Should().NotBeNull();
        validated!.UserId.Should().Be(principal.UserId);
        validated.Email.Should().Be(principal.Email);
    }

    [Fact]
    public void Validate_rejects_tampered_payload()
    {
        var svc = CreateService();
        var now = new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero);
        var token = svc.IssueToken(new AuthPrincipal(Guid.NewGuid(), "u@x.com", "x", now.AddHours(1)));

        var tampered = token.Token + "z";
        svc.ValidateToken(tampered, now).Should().BeNull();
    }

    [Fact]
    public void Validate_rejects_expired_token()
    {
        var svc = CreateService();
        var issuedAt = new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero);
        var token = svc.IssueToken(new AuthPrincipal(Guid.NewGuid(), "u@x.com", "x", issuedAt.AddMinutes(10)));

        var futureNow = issuedAt.AddHours(1);
        svc.ValidateToken(token.Token, futureNow).Should().BeNull();
    }

    [Fact]
    public void Validate_rejects_token_signed_with_different_key()
    {
        var svcA = CreateService("AAAA1111BBBB2222CCCC3333DDDD4444");
        var svcB = CreateService("ZZZZ9999YYYY8888XXXX7777WWWW6666");
        var now = new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero);
        var token = svcA.IssueToken(new AuthPrincipal(Guid.NewGuid(), "u@x.com", "x", now.AddHours(1)));

        svcB.ValidateToken(token.Token, now).Should().BeNull();
    }
}
