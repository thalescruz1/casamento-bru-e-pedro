using System.Text.RegularExpressions;
using Casamento.Domain.Common;

namespace Casamento.Domain.Rsvps.ValueObjects;

public readonly partial record struct Email
{
    private static readonly Regex Pattern = EmailRegex();

    public string Value { get; }

    private Email(string value) => Value = value;

    public static Email Create(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new DomainException("E-mail é obrigatório.");
        }

        var trimmed = raw.Trim().ToLowerInvariant();

        if (trimmed.Length > 254 || !Pattern.IsMatch(trimmed))
        {
            throw new DomainException("E-mail inválido.");
        }

        return new Email(trimmed);
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();
}
