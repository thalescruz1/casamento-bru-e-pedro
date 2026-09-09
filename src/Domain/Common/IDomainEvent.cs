namespace Casamento.Domain.Common;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
