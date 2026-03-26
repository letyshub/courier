using Courier.Commands;
using Courier.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Courier.Tests;

public class CommandHandlingTests
{
    // ── Fakes ──────────────────────────────────────────────────────────────

    private record CreateOrderCommand(string Product, int Quantity) : ICommand<int>;
    private record CancelOrderCommand(int OrderId) : ICommand;

    private class CreateOrderHandler : ICommandHandler<CreateOrderCommand, int>
    {
        public Task<int> HandleAsync(CreateOrderCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(command.Quantity * 10); // fake order id
    }

    private class CancelOrderHandler : ICommandHandler<CancelOrderCommand>
    {
        public static bool WasCalled;

        public Task<Unit> HandleAsync(CancelOrderCommand command, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return Unit.Task;
        }
    }

    private record FailingCommand : ICommand;

    private class FailingCommandHandler : ICommandHandler<FailingCommand>
    {
        public Task<Unit> HandleAsync(FailingCommand command, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Boom");
    }

    // ── Tests ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_CommandWithResult_ReturnsHandlerResult()
    {
        // Arrange
        var dispatcher = DispatcherFixture.Build();
        var command = new CreateOrderCommand("Widget", 3);

        // Act
        var result = await dispatcher.SendAsync(command);

        // Assert
        Assert.Equal(30, result);
    }

    [Fact]
    public async Task SendAsync_VoidCommand_InvokesHandler()
    {
        // Arrange
        CancelOrderHandler.WasCalled = false;
        var dispatcher = DispatcherFixture.Build();

        // Act
        await dispatcher.SendAsync(new CancelOrderCommand(42));

        // Assert
        Assert.True(CancelOrderHandler.WasCalled);
    }

    [Fact]
    public async Task SendAsync_NoHandlerRegistered_ThrowsInvalidOperationException()
    {
        // Arrange — create dispatcher without scanning this assembly
        var services = new ServiceCollection();
        services.AddCourier(); // no assembly
        var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IDispatcher>();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dispatcher.SendAsync(new CreateOrderCommand("x", 1)));
    }

    [Fact]
    public async Task SendAsync_HandlerThrows_ExceptionPropagates()
    {
        // Arrange
        var dispatcher = DispatcherFixture.Build();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dispatcher.SendAsync(new FailingCommand()));
    }

    [Fact]
    public async Task SendAsync_NullCommand_ThrowsArgumentNullException()
    {
        var dispatcher = DispatcherFixture.Build();
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            dispatcher.SendAsync<int>(null!));
    }
}
