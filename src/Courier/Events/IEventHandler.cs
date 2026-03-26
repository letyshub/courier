namespace Courier.Events;

/// <summary>
/// Handles events of type <typeparamref name="TEvent"/>.
/// Multiple handlers can be registered for the same event type.
/// </summary>
/// <typeparam name="TEvent">The event type to handle.</typeparam>
public interface IEventHandler<in TEvent> where TEvent : IEvent
{
    /// <summary>Reacts to the emitted event.</summary>
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
