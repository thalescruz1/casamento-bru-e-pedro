using Casamento.Application.Abstractions;
using Casamento.Application.Auth.Commands.Login;
using Casamento.Domain.Auth;
using Casamento.Domain.Auth.ValueObjects;
using Casamento.Domain.Rsvps.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Casamento.Application.Tests.Auth;

public sealed class LoginHandlerTests
{
    private readonly IAdminUserRepository _repository = Substitute.For<IAdminUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IAuthTokenService _tokens = Substitute.For<IAuthTokenService>();
    private readonly TestClock _clock = new();

    private static AdminUser CreateUser()
    {
        return AdminUser.Create(
            Email.Create("thales@casamento.test"),
            "Thales",
            HashedPassword.FromEncoded("pbkdf2-sha512$100000$AAA=$BBB="),
            new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero));
    }

    private LoginHandler CreateHandler() =>
        new(_repository, _hasher, _tokens, _clock, NullLogger<LoginHandler>.Instance);

    [Fact]
    public async Task Returns_token_when_credentials_ok()
    {
        var user = CreateUser();
        _repository.FindByEmailAsync("thales@casamento.test", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify(user.PasswordHash, "correct").Returns(true);
        _tokens.SessionLifetime.Returns(TimeSpan.FromHours(1));
        _tokens.IssueToken(Arg.Any<AuthPrincipal>())
            .Returns(new AuthTokenResult("token.signed", _clock.UtcNow.AddHours(1)));

        var result = await CreateHandler().Handle(
            new LoginCommand("Thales@casamento.test", "correct", "hash"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Token.Should().Be("token.signed");
        result.Value.User.DisplayName.Should().Be("Thales");
        await _repository.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_with_wrong_password()
    {
        var user = CreateUser();
        _repository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify(Arg.Any<HashedPassword>(), Arg.Any<string>()).Returns(false);

        var result = await CreateHandler().Handle(
            new LoginCommand("thales@casamento.test", "wrong", "hash"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation");
        user.FailedAttempts.Should().Be(1);
    }

    [Fact]
    public async Task Rejects_with_unknown_email_without_leaking()
    {
        _repository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((AdminUser?)null);

        var result = await CreateHandler().Handle(
            new LoginCommand("anyone@example.com", "whatever", "hash"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("inválidos");
    }

    [Fact]
    public async Task Locks_account_after_max_failed_attempts()
    {
        var user = CreateUser();
        _repository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify(Arg.Any<HashedPassword>(), Arg.Any<string>()).Returns(false);

        var handler = CreateHandler();
        for (var i = 0; i < 5; i++)
        {
            await handler.Handle(new LoginCommand("thales@casamento.test", "wrong", "hash"), CancellationToken.None);
        }

        user.IsLocked(_clock.UtcNow).Should().BeTrue();

        var again = await handler.Handle(new LoginCommand("thales@casamento.test", "wrong", "hash"), CancellationToken.None);
        again.Error.Code.Should().Be("rate_limited");
    }
}
