using Casamento.Domain.Rsvps;
using Casamento.Domain.Rsvps.ValueObjects;
using Newtonsoft.Json;

namespace Casamento.Infrastructure.Cosmos.Documents;

internal sealed class RsvpDocument
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("type")]
    public string Type { get; set; } = "rsvp";

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("email")]
    public string Email { get; set; } = string.Empty;

    [JsonProperty("phone")]
    public string Phone { get; set; } = string.Empty;

    [JsonProperty("attend")]
    public Attendance Attend { get; set; }

    [JsonProperty("guests")]
    public int Guests { get; set; }

    [JsonProperty("guestNames")]
    public string? GuestNames { get; set; }

    [JsonProperty("restrictions")]
    public string? Restrictions { get; set; }

    [JsonProperty("submittedAt")]
    public DateTimeOffset SubmittedAt { get; set; }

    [JsonProperty("ipHash")]
    public string IpHash { get; set; } = string.Empty;

    public static RsvpDocument FromAggregate(Rsvp rsvp) => new()
    {
        Id = rsvp.Id.ToString("N"),
        Type = "rsvp",
        Name = rsvp.Name,
        Email = rsvp.Email.Value,
        Phone = rsvp.Phone,
        Attend = rsvp.Attend,
        Guests = rsvp.Guests,
        GuestNames = rsvp.GuestNames,
        Restrictions = rsvp.Restrictions,
        SubmittedAt = rsvp.SubmittedAt,
        IpHash = rsvp.IpHash
    };

    public Rsvp ToAggregate()
    {
        var ctor = typeof(Rsvp).GetConstructor(
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            System.Type.EmptyTypes)!;
        var rsvp = (Rsvp)ctor.Invoke(null);

        SetPrivate(rsvp, nameof(Rsvp.Id), Guid.ParseExact(Id, "N"));
        SetPrivate(rsvp, nameof(Rsvp.Name), Name);
        SetPrivate(rsvp, nameof(Rsvp.Email), Casamento.Domain.Rsvps.ValueObjects.Email.Create(Email));
        SetPrivate(rsvp, nameof(Rsvp.Phone), Phone ?? string.Empty);
        SetPrivate(rsvp, nameof(Rsvp.Attend), Attend);
        SetPrivate(rsvp, nameof(Rsvp.Guests), Guests);
        SetPrivate(rsvp, nameof(Rsvp.GuestNames), GuestNames);
        SetPrivate(rsvp, nameof(Rsvp.Restrictions), Restrictions);
        SetPrivate(rsvp, nameof(Rsvp.SubmittedAt), SubmittedAt);
        SetPrivate(rsvp, nameof(Rsvp.IpHash), IpHash);
        return rsvp;
    }

    private static void SetPrivate(object target, string propertyName, object? value)
    {
        var prop = target.GetType().GetProperty(propertyName)!;
        prop.GetSetMethod(nonPublic: true)!.Invoke(target, [value]);
    }
}
