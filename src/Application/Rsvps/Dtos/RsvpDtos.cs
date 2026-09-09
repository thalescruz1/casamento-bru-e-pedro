using Casamento.Domain.Rsvps;

namespace Casamento.Application.Rsvps.Dtos;

public sealed record RsvpConfirmationDto(Guid Id, string Name, Attendance Attend, DateTimeOffset SubmittedAt);

public sealed record RsvpListItemDto(
    Guid Id,
    string Name,
    string Email,
    string Phone,
    Attendance Attend,
    int Guests,
    string? GuestNames,
    string? Restrictions,
    DateTimeOffset SubmittedAt);

public sealed record RsvpListDto(IReadOnlyList<RsvpListItemDto> Items, int Total);
