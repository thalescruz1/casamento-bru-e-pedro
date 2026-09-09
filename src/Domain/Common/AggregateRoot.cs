namespace Casamento.Domain.Common;

public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _events = [];

    public IReadOnlyList<IDomainEvent> DomainEvents => _events;

    protected void Raise(IDomainEvent evt) => _events.Add(evt);

    public void ClearEvents() => _events.Clear();
}
