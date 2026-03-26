# Events

Events represent **things that have already happened** in the system. Multiple handlers can subscribe to the same event.

## Defining an Event

Implement `IEvent`:

```csharp
using Courier.Events;

record OrderShippedEvent(int OrderId, string TrackingCode) : IEvent;
```

## Implementing Handlers

Any number of handlers can react to the same event:

```csharp
class ShippingNotificationHandler : IEventHandler<OrderShippedEvent>
{
    private readonly IEmailService _email;

    public ShippingNotificationHandler(IEmailService email) => _email = email;

    public async Task HandleAsync(OrderShippedEvent @event, CancellationToken ct = default)
        => await _email.SendShippingConfirmationAsync(@event.OrderId, @event.TrackingCode, ct);
}

class ShippingAuditHandler : IEventHandler<OrderShippedEvent>
{
    private readonly IAuditLog _audit;

    public ShippingAuditHandler(IAuditLog audit) => _audit = audit;

    public Task HandleAsync(OrderShippedEvent @event, CancellationToken ct = default)
        => _audit.RecordAsync($"Order {@event.OrderId} shipped via {@event.TrackingCode}", ct);
}
```

## Emitting an Event

```csharp
await _dispatcher.EmitAsync(new OrderShippedEvent(orderId, "TRK-001"));
```

## Error Handling

All handlers are always invoked. If one or more handlers throw, their exceptions are collected and re-thrown as a single `AggregateException` after all handlers have run:

```csharp
try
{
    await _dispatcher.EmitAsync(new OrderShippedEvent(orderId, "TRK-001"));
}
catch (AggregateException ex)
{
    foreach (var inner in ex.InnerExceptions)
        _logger.LogError(inner, "Event handler failed");
}
```

## No Handlers

Emitting an event with no registered handlers is a no-op — no exception is thrown.
