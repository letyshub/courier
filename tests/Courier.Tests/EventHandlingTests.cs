using Courier.Events;

namespace Courier.Tests;

public class EventHandlingTests
{
    // ── Fakes ──────────────────────────────────────────────────────────────

    private record OrderShippedEvent(int OrderId, string TrackingCode) : IEvent;

    private class ShippingNotificationHandler : IEventHandler<OrderShippedEvent>
    {
        public static readonly List<int> HandledOrderIds = [];

        public Task HandleAsync(OrderShippedEvent @event, CancellationToken cancellationToken = default)
        {
            HandledOrderIds.Add(@event.OrderId);
            return Task.CompletedTask;
        }
    }

    private class ShippingAuditHandler : IEventHandler<OrderShippedEvent>
    {
        public static bool AuditRecorded;

        public Task HandleAsync(OrderShippedEvent @event, CancellationToken cancellationToken = default)
        {
            AuditRecorded = true;
            return Task.CompletedTask;
        }
    }

    private record NoHandlersEvent : IEvent;

    private record FaultyEvent : IEvent;

    private class FaultyEventHandler : IEventHandler<FaultyEvent>
    {
        public Task HandleAsync(FaultyEvent @event, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("handler error");
    }

    // ── Tests ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task EmitAsync_SingleHandler_HandlerIsCalled()
    {
        // Arrange
        ShippingNotificationHandler.HandledOrderIds.Clear();
        var dispatcher = DispatcherFixture.Build();

        // Act
        await dispatcher.EmitAsync(new OrderShippedEvent(99, "TRK-001"));

        // Assert
        Assert.Contains(99, ShippingNotificationHandler.HandledOrderIds);
    }

    [Fact]
    public async Task EmitAsync_MultipleHandlers_AllHandlersCalled()
    {
        // Arrange
        ShippingNotificationHandler.HandledOrderIds.Clear();
        ShippingAuditHandler.AuditRecorded = false;
        var dispatcher = DispatcherFixture.Build();

        // Act
        await dispatcher.EmitAsync(new OrderShippedEvent(5, "TRK-002"));

        // Assert
        Assert.Contains(5, ShippingNotificationHandler.HandledOrderIds);
        Assert.True(ShippingAuditHandler.AuditRecorded);
    }

    [Fact]
    public async Task EmitAsync_NoHandlersRegistered_DoesNotThrow()
    {
        // Arrange
        var dispatcher = DispatcherFixture.Build();

        // Act & Assert (should complete silently)
        await dispatcher.EmitAsync(new NoHandlersEvent());
    }

    [Fact]
    public async Task EmitAsync_HandlerThrows_WrapsInAggregateException()
    {
        // Arrange
        var dispatcher = DispatcherFixture.Build();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AggregateException>(() =>
            dispatcher.EmitAsync(new FaultyEvent()));

        Assert.Single(ex.InnerExceptions);
        Assert.IsType<InvalidOperationException>(ex.InnerExceptions[0]);
    }

    [Fact]
    public async Task EmitAsync_NullEvent_ThrowsArgumentNullException()
    {
        var dispatcher = DispatcherFixture.Build();
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            dispatcher.EmitAsync<OrderShippedEvent>(null!));
    }
}
