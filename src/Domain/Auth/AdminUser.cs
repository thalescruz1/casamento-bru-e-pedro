using Casamento.Domain.Auth.ValueObjects;
using Casamento.Domain.Common;
using Casamento.Domain.Rsvps.ValueObjects;

namespace Casamento.Domain.Auth;

public sealed class AdminUser
{
    public Guid Id { get; private set; }
    public Email Email { get; private set; }
    public string DisplayName { get; private set; } = default!;
    public HashedPassword PasswordHash { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }
    public int FailedAttempts { get; private set; }
    public DateTimeOffset? LockedUntil { get; private set; }

    private AdminUser() { }

    public static AdminUser Create(Email email, string displayName, HashedPassword hash, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        var trimmed = displayName.Trim();
        if (trimmed.Length is < 2 or > 80)
        {
            throw new DomainException("Nome de exibição inválido.");
        }

        return new AdminUser
        {
            Id = GuidV7.NewGuid(),
            Email = email,
            DisplayName = trimmed,
            PasswordHash = hash,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void RegisterSuccessfulLogin(DateTimeOffset now)
    {
        LastLoginAt = now;
        FailedAttempts = 0;
        LockedUntil = null;
        UpdatedAt = now;
    }

    public void RegisterFailedLogin(DateTimeOffset now, int maxAttempts, TimeSpan lockoutDuration)
    {
        FailedAttempts++;
        if (FailedAttempts >= maxAttempts)
        {
            LockedUntil = now + lockoutDuration;
        }
        UpdatedAt = now;
    }

    public bool IsLocked(DateTimeOffset now) => LockedUntil is { } until && until > now;

    public void ChangePassword(HashedPassword newHash, DateTimeOffset now)
    {
        PasswordHash = newHash;
        FailedAttempts = 0;
        LockedUntil = null;
        UpdatedAt = now;
    }
}
