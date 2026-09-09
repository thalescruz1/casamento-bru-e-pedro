using Casamento.Domain.Common;

namespace Casamento.Domain.Rsvps.Events;

public sealed record RsvpSubmitted(Guid RsvpId, Attendance Attend, DateTimeOffset OccurredAt) : IDomainEvent;
