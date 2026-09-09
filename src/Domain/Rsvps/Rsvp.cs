using Casamento.Domain.Common;
using Casamento.Domain.Rsvps.Events;
using Casamento.Domain.Rsvps.ValueObjects;

namespace Casamento.Domain.Rsvps;

public sealed class Rsvp : AggregateRoot
{
    public const int MaxNameLength = 120;
    public const int MinNameLength = 2;
    public const int MaxRestrictionsLength = 500;
    public const int MaxGuestNamesLength = 240;
    public const int MaxAdditionalGuests = 1;

    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public Email Email { get; private set; }
    public string Phone { get; private set; } = default!;
    public Attendance Attend { get; private set; }
    public int Guests { get; private set; }
    public string? GuestNames { get; private set; }
    public string? Restrictions { get; private set; }
    public DateTimeOffset SubmittedAt { get; private set; }
    public string IpHash { get; private set; } = default!;

    private Rsvp() { }

    public static Rsvp Submit(
        string name,
        Email email,
        string phone,
        Attendance attend,
        int guests,
        string? guestNames,
        string? restrictions,
        string ipHash,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(ipHash);

        var trimmedName = name.Trim();
        if (trimmedName.Length is < MinNameLength or > MaxNameLength)
        {
            throw new DomainException($"Nome deve ter entre {MinNameLength} e {MaxNameLength} caracteres.");
        }

        var normalizedPhone = NormalizePhone(phone);

        if (guests is < 0 or > MaxAdditionalGuests)
        {
            throw new DomainException($"Número de acompanhantes inválido (0-{MaxAdditionalGuests}).");
        }

        if (attend == Attendance.Nao && guests > 0)
        {
            throw new DomainException("Quem não comparece não leva acompanhante.");
        }

        var normalizedGuestNames = string.IsNullOrWhiteSpace(guestNames) ? null : guestNames.Trim();
        if (normalizedGuestNames is { Length: > MaxGuestNamesLength })
        {
            throw new DomainException($"Nome dos acompanhantes excede {MaxGuestNamesLength} caracteres.");
        }

        if (guests > 0 && normalizedGuestNames is null)
        {
            throw new DomainException("Informe o nome do acompanhante.");
        }

        if (guests == 0 && normalizedGuestNames is not null)
        {
            normalizedGuestNames = null;
        }

        if (restrictions is { Length: > MaxRestrictionsLength })
        {
            throw new DomainException($"Observação excede {MaxRestrictionsLength} caracteres.");
        }

        var rsvp = new Rsvp
        {
            Id = GuidV7.NewGuid(),
            Name = trimmedName,
            Email = email,
            Phone = normalizedPhone,
            Attend = attend,
            Guests = guests,
            GuestNames = normalizedGuestNames,
            Restrictions = string.IsNullOrWhiteSpace(restrictions) ? null : restrictions.Trim(),
            SubmittedAt = now,
            IpHash = ipHash
        };

        rsvp.Raise(new RsvpSubmitted(rsvp.Id, attend, now));
        return rsvp;
    }

    private static string NormalizePhone(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new DomainException("Telefone é obrigatório.");
        }

        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.Length is < 10 or > 13)
        {
            throw new DomainException("Telefone inválido.");
        }
        return digits;
    }
}
