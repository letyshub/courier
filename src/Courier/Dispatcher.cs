using System.Runtime.ExceptionServices;
using Courier.Commands;
using Courier.Events;
using Courier.Pipeline;
using Courier.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Courier;

/// <summary>
/// Default implementation of <see cref="IDispatcher"/>.
/// Resolves handlers and pipeline steps from the <see cref="IServiceProvider"/>.
/// </summary>
internal sealed class Dispatcher : IDispatcher
{
    private readonly IServiceProvider _services;

    public Dispatcher(IServiceProvider services)
    {
        _services = services;
    }

    /// <inheritdoc/>
    public Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult));
        var handler = _services.GetService(handlerType)
            ?? throw new InvalidOperationException(
                $"No handler registered for command '{command.GetType().Name}'. " +
                $"Expected registration of '{handlerType.Name}'.");

        var steps = ResolveSteps<TResult>(command.GetType());

        Func<Task<TResult>> execute = () => InvokeCommandHandler<TResult>(handler, command, cancellationToken);

        return BuildPipeline(steps, command, execute, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task SendAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        await SendAsync<Unit>(command, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<TResult> QueryAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResult));
        var handler = _services.GetService(handlerType)
            ?? throw new InvalidOperationException(
                $"No handler registered for query '{query.GetType().Name}'. " +
                $"Expected registration of '{handlerType.Name}'.");

        var steps = ResolveSteps<TResult>(query.GetType());

        Func<Task<TResult>> execute = () => InvokeQueryHandler<TResult>(handler, query, cancellationToken);

        return BuildPipeline(steps, query, execute, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task EmitAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IEvent
    {
        ArgumentNullException.ThrowIfNull(@event);

        var handlers = _services.GetServices<IEventHandler<TEvent>>().ToList();

        if (handlers.Count == 0)
            return;

        var exceptions = new List<Exception>();

        foreach (var handler in handlers)
        {
            try
            {
                await handler.HandleAsync(@event, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        if (exceptions.Count > 0)
            throw new AggregateException($"One or more event handlers for '{typeof(TEvent).Name}' threw exceptions.", exceptions);
    }

    // --- private helpers ---

    private IList<object> ResolveSteps<TResult>(Type inputType)
    {
        var stepType = typeof(IPipelineStep<,>).MakeGenericType(inputType, typeof(TResult));
        return _services.GetServices(stepType).OfType<object>().ToList();
    }

    private static Task<TResult> BuildPipeline<TResult>(
        IList<object> steps,
        object input,
        Func<Task<TResult>> handler,
        CancellationToken cancellationToken)
    {
        if (steps.Count == 0)
            return handler();

        // Build the chain from last to first so the first step runs outermost.
        var next = handler;
        for (var i = steps.Count - 1; i >= 0; i--)
        {
            var step = steps[i];
            var captured = next;
            next = () => InvokePipelineStep<TResult>(step, input, captured, cancellationToken);
        }

        return next();
    }

    private static Task<TResult> InvokeCommandHandler<TResult>(object handler, object command, CancellationToken ct)
    {
        var method = handler.GetType().GetMethod("HandleAsync")!;
        return InvokeReflected<TResult>(method, handler, [command, ct]);
    }

    private static Task<TResult> InvokeQueryHandler<TResult>(object handler, object query, CancellationToken ct)
    {
        var method = handler.GetType().GetMethod("HandleAsync")!;
        return InvokeReflected<TResult>(method, handler, [query, ct]);
    }

    private static Task<TResult> InvokePipelineStep<TResult>(object step, object input, Func<Task<TResult>> next, CancellationToken ct)
    {
        var method = step.GetType().GetMethod("ExecuteAsync")!;
        return InvokeReflected<TResult>(method, step, [input, next, ct]);
    }

    private static Task<TResult> InvokeReflected<TResult>(
        System.Reflection.MethodInfo method, object target, object?[] args)
    {
        try
        {
            return (Task<TResult>)method.Invoke(target, args)!;
        }
        catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw; // unreachable
        }
    }
}
