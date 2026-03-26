using Courier.Commands;
using Courier.DependencyInjection;
using Courier.Events;
using Courier.Pipeline;
using Courier.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Courier.Tests;

/// <summary>
/// Verifies that CancellationToken is correctly forwarded to command handlers,
/// query handlers, event handlers and pipeline steps.
/// </summary>
public class CancellationTests
{
    // ── Fakes ──────────────────────────────────────────────────────────────

    private record CancellableCommand : ICommand<string>;
    private record CancellableQuery : IQuery<string>;
    private record CancellableVoidCommand : ICommand;
    private record CancellableEvent : IEvent;

    private class CancellableCommandHandler : ICommandHandler<CancellableCommand, string>
    {
        public Task<string> HandleAsync(CancellableCommand command, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult("ok");
        }
    }

    private class CancellableQueryHandler : IQueryHandler<CancellableQuery, string>
    {
        public Task<string> HandleAsync(CancellableQuery query, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult("ok");
        }
    }

    private class CancellableVoidCommandHandler : ICommandHandler<CancellableVoidCommand>
    {
        public Task<Unit> HandleAsync(CancellableVoidCommand command, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return Unit.Task;
        }
    }

    private class CancellableEventHandler : IEventHandler<CancellableEvent>
    {
        public Task HandleAsync(CancellableEvent @event, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    /// <summary>Pipeline step that checks the token before calling next.</summary>
    private class CancellableStep<TInput, TOutput> : IPipelineStep<TInput, TOutput>
    {
        public Task<TOutput> ExecuteAsync(TInput input, Func<Task<TOutput>> next, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return next();
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static CancellationToken CancelledToken()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        return cts.Token;
    }

    private static IDispatcher Build(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddCourier(typeof(CancellationTests).Assembly);
        configure?.Invoke(services);
        var scope = services.BuildServiceProvider().CreateScope();
        return scope.ServiceProvider.GetRequiredService<IDispatcher>();
    }

    // ── Command handler tests ───────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_CommandHandler_CancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var dispatcher = Build();
        var ct = CancelledToken();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            dispatcher.SendAsync(new CancellableCommand(), ct));
    }

    [Fact]
    public async Task SendAsync_VoidCommandHandler_CancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var dispatcher = Build();
        var ct = CancelledToken();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            dispatcher.SendAsync(new CancellableVoidCommand(), ct));
    }

    [Fact]
    public async Task SendAsync_CommandHandler_AsyncCancellation_ThrowsOperationCanceledException()
    {
        // Arrange — handler awaits a delay that will be cancelled mid-flight
        var services = new ServiceCollection();
        services.AddCourier();
        services.AddScoped<ICommandHandler<CancellableCommand, string>, DelayingCommandHandler>();
        var scope = services.BuildServiceProvider().CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

        using var cts = new CancellationTokenSource(millisecondsDelay: 50);

        // Act — Task.Delay cancellation produces TaskCanceledException (subtype of OperationCanceledException)
        var ex = await Record.ExceptionAsync(() => dispatcher.SendAsync(new CancellableCommand(), cts.Token));

        // Assert
        Assert.IsAssignableFrom<OperationCanceledException>(ex);
    }

    // ── Query handler tests ────────────────────────────────────────────────

    [Fact]
    public async Task QueryAsync_QueryHandler_CancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var dispatcher = Build();
        var ct = CancelledToken();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            dispatcher.QueryAsync(new CancellableQuery(), ct));
    }

    [Fact]
    public async Task QueryAsync_QueryHandler_AsyncCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCourier();
        services.AddScoped<IQueryHandler<CancellableQuery, string>, DelayingQueryHandler>();
        var scope = services.BuildServiceProvider().CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

        using var cts = new CancellationTokenSource(millisecondsDelay: 50);

        // Act — Task.Delay cancellation produces TaskCanceledException (subtype of OperationCanceledException)
        var ex = await Record.ExceptionAsync(() => dispatcher.QueryAsync(new CancellableQuery(), cts.Token));

        // Assert
        Assert.IsAssignableFrom<OperationCanceledException>(ex);
    }

    // ── Event handler tests ────────────────────────────────────────────────

    [Fact]
    public async Task EmitAsync_EventHandler_CancelledToken_WrapsInAggregateException()
    {
        // Arrange — EmitAsync catches all handler exceptions (including cancellation)
        // and collects them into AggregateException so all handlers always run.
        var dispatcher = Build();
        var ct = CancelledToken();

        // Act
        var ex = await Assert.ThrowsAsync<AggregateException>(() =>
            dispatcher.EmitAsync(new CancellableEvent(), ct));

        // Assert — inner exception is the original OperationCanceledException
        Assert.Single(ex.InnerExceptions);
        Assert.IsAssignableFrom<OperationCanceledException>(ex.InnerExceptions[0]);
    }

    // ── Pipeline step tests ────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_PipelineStep_CancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange — step checks token before calling handler
        var dispatcher = Build(services =>
            services.AddScoped<
                IPipelineStep<CancellableCommand, string>,
                CancellableStep<CancellableCommand, string>>());

        var ct = CancelledToken();

        // Act & Assert — cancellation fires in the step, before handler is reached
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            dispatcher.SendAsync(new CancellableCommand(), ct));
    }

    [Fact]
    public async Task QueryAsync_PipelineStep_CancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var dispatcher = Build(services =>
            services.AddScoped<
                IPipelineStep<CancellableQuery, string>,
                CancellableStep<CancellableQuery, string>>());

        var ct = CancelledToken();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            dispatcher.QueryAsync(new CancellableQuery(), ct));
    }

    // ── Async-delay fakes (used for in-flight cancellation tests) ──────────

    private class DelayingCommandHandler : ICommandHandler<CancellableCommand, string>
    {
        public async Task<string> HandleAsync(CancellableCommand command, CancellationToken ct = default)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), ct); // cancelled long before this finishes
            return "never";
        }
    }

    private class DelayingQueryHandler : IQueryHandler<CancellableQuery, string>
    {
        public async Task<string> HandleAsync(CancellableQuery query, CancellationToken ct = default)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), ct);
            return "never";
        }
    }
}
