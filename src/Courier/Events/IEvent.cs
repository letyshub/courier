namespace Courier.Events;

/// <summary>
/// Marks a domain event — something that has already happened in the system.
/// Multiple handlers can subscribe to the same event.
/// </summary>
public interface IEvent { }
