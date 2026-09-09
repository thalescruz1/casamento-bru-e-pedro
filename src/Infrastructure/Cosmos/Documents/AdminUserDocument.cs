using Casamento.Domain.Auth;
using Casamento.Domain.Auth.ValueObjects;
using Casamento.Domain.Rsvps.ValueObjects;
using Newtonsoft.Json;

namespace Casamento.Infrastructure.Cosmos.Documents;

internal sealed class AdminUserDocument
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("type")]
    public string Type { get; set; } = "admin";

    [JsonProperty("email")]
    public string Email { get; set; } = string.Empty;

    [JsonProperty("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonProperty("passwordHash")]
    public string PasswordHash { get; set; } = string.Empty;

    [JsonProperty("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }

    [JsonProperty("updatedAt")]
    public DateTimeOffset UpdatedAt { get; set; }

    [JsonProperty("lastLoginAt")]
    public DateTimeOffset? LastLoginAt { get; set; }

    [JsonProperty("failedAttempts")]
    public int FailedAttempts { get; set; }

    [JsonProperty("lockedUntil")]
    public DateTimeOffset? LockedUntil { get; set; }

    public static AdminUserDocument FromAggregate(AdminUser user) => new()
    {
        Id = user.Id.ToString("N"),
        Type = "admin",
        Email = user.Email.Value,
        DisplayName = user.DisplayName,
        PasswordHash = user.PasswordHash.Encoded,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt,
        LastLoginAt = user.LastLoginAt,
        FailedAttempts = user.FailedAttempts,
        LockedUntil = user.LockedUntil
    };

    public AdminUser ToAggregate()
    {
        var ctor = typeof(AdminUser).GetConstructor(
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            System.Type.EmptyTypes)!;
        var user = (AdminUser)ctor.Invoke(null);

        SetPrivate(user, nameof(AdminUser.Id), Guid.ParseExact(Id, "N"));
        SetPrivate(user, nameof(AdminUser.Email), Casamento.Domain.Rsvps.ValueObjects.Email.Create(Email));
        SetPrivate(user, nameof(AdminUser.DisplayName), DisplayName);
        SetPrivate(user, nameof(AdminUser.PasswordHash), HashedPassword.FromEncoded(PasswordHash));
        SetPrivate(user, nameof(AdminUser.CreatedAt), CreatedAt);
        SetPrivate(user, nameof(AdminUser.UpdatedAt), UpdatedAt);
        SetPrivate(user, nameof(AdminUser.LastLoginAt), LastLoginAt);
        SetPrivate(user, nameof(AdminUser.FailedAttempts), FailedAttempts);
        SetPrivate(user, nameof(AdminUser.LockedUntil), LockedUntil);
        return user;
    }

    private static void SetPrivate(object target, string propertyName, object? value)
    {
        var prop = target.GetType().GetProperty(propertyName)!;
        prop.GetSetMethod(nonPublic: true)!.Invoke(target, [value]);
    }
}
