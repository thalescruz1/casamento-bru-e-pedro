using Casamento.Domain.Common;

namespace Casamento.Domain.Auth.ValueObjects;

/// <summary>
/// Formato serializado no Cosmos: "pbkdf2-sha512${iterations}${base64salt}${base64hash}".
/// </summary>
public readonly record struct HashedPassword
{
    public string Encoded { get; }

    private HashedPassword(string encoded) => Encoded = encoded;

    public static HashedPassword FromEncoded(string encoded)
    {
        if (string.IsNullOrWhiteSpace(encoded))
        {
            throw new DomainException("Hash de senha inválido.");
        }

        var parts = encoded.Split('$');
        if (parts.Length != 4 || parts[0] != "pbkdf2-sha512")
        {
            throw new DomainException("Formato de hash de senha desconhecido.");
        }

        return new HashedPassword(encoded);
    }

    public override string ToString() => Encoded;
}
