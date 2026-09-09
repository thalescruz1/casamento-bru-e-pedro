using Casamento.Application.Common;
using Casamento.Application.Rsvps.Dtos;
using Casamento.Domain.Rsvps;
using MediatR;

namespace Casamento.Application.Rsvps.Commands.SubmitRsvp;

public sealed record SubmitRsvpCommand(
    string Name,
    string Email,
    string Phone,
    Attendance Attend,
    int Guests,
    string? GuestNames,
    string? Restrictions,
    string IpHash) : IRequest<Result<RsvpConfirmationDto>>;
