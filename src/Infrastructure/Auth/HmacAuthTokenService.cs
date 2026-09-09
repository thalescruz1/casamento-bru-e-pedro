using System.Security.Cryptography;
using System.Text;
using Casamento.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace Casamento.Infrastructure.Auth;

internal sealed class HmacAuthTokenService(IOptions<AuthOptions> options) : IAuthTokenService
{
    private const char Separator = '.';
    private const char FieldSeparator = '|';
    private readonly AuthOptions _options = options.Value;

    public TimeSpan SessionLifetime => TimeSpan.FromHours(_options.SessionLifetimeHours);

    public AuthTokenResult IssueToken(AuthPrincipal principal)
    {
        var payload = $"{principal.UserId:N}{FieldSeparator}{principal.Email}{FieldSeparator}{principal.ExpiresAt.ToUnixTimeSeconds()}";
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var signature = ComputeSignature(payloadBytes);

        var token = $"{Base64Url(payloadBytes)}{Separator}{Base64Url(signature)}";
        return new AuthTokenResult(token, principal.ExpiresAt);
    }

    public AuthPrincipal? ValidateToken(string? token, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var parts = token.Split(Separator);
        if (parts.Length != 2)
        {
            return null;
        }

        byte[] payloadBytes;
        byte[] providedSignature;
        try
        {
            payloadBytes = Base64UrlDecode(parts[0]);
            providedSignature = Base64UrlDecode(parts[1]);
        }
        catch (FormatException)
        {
            return null;
        }

        var expected = ComputeSignature(payloadBytes);
        if (!CryptographicOperations.FixedTimeEquals(providedSignature, expected))
        {
            return null;
        }

        var payload = Encoding.UTF8.GetString(payloadBytes);
        var fields = payload.Split(FieldSeparator);
        if (fields.Length != 3)
        {
            return null;
        }

        if (!Guid.TryParseExact(fields[0], "N", out var userId))
        {
            return null;
        }

        if (!long.TryParse(fields[2], System.Globalization.CultureInfo.InvariantCulture, out var expUnix))
        {
            return null;
        }

        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expUnix);
        if (expiresAt <= now)
        {
            return null;
        }

        return new AuthPrincipal(userId, fields[1], string.Empty, expiresAt);
    }

    private byte[] ComputeSignature(byte[] payload)
    {
        var key = Encoding.UTF8.GetBytes(_options.TokenSigningKey);
        return HMACSHA256.HashData(key, payload);
    }

    private static string Base64Url(byte[] data) =>
        Convert.ToBase64String(data)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }
        return Convert.FromBase64String(padded);
    }
}
