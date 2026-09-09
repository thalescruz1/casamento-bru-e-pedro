using System.Security.Cryptography;
using System.Text;
using Casamento.Application.Abstractions;
using Casamento.Domain.Auth.ValueObjects;

namespace Casamento.Infrastructure.Auth;

internal sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const string Scheme = "pbkdf2-sha512";
    private const int SaltSize = 16;
    private const int HashSize = 64;
    private const int Iterations = 210_000;

    public HashedPassword Hash(string plaintext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintext);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(plaintext),
            salt,
            Iterations,
            HashAlgorithmName.SHA512,
            HashSize);

        var encoded = $"{Scheme}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        return HashedPassword.FromEncoded(encoded);
    }

    public bool Verify(HashedPassword encoded, string plaintext)
    {
        if (string.IsNullOrWhiteSpace(plaintext))
        {
            return false;
        }

        var parts = encoded.Encoded.Split('$');
        if (parts.Length != 4 || parts[0] != Scheme)
        {
            return false;
        }

        if (!int.TryParse(parts[1], System.Globalization.CultureInfo.InvariantCulture, out var iterations))
        {
            return false;
        }

        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expected = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(plaintext),
            salt,
            iterations,
            HashAlgorithmName.SHA512,
            expected.Length);

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
