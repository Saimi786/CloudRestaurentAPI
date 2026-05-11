namespace CloudRestaurent.Domain.Common;

public abstract record DomainEvent(DateTimeOffset OccurredOn) : IDomainEvent
{
    protected DomainEvent() : this(DateTimeOffset.UtcNow) { }
}

public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}
