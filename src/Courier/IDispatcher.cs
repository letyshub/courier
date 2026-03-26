using Courier.Commands;
using Courier.Events;
using Courier.Queries;

namespace Courier;

/// <summary>
/// The central entry point for dispatching commands, queries and events.
/// </summary>
public interface IDispatcher
{
    /// <summary>Dispatches a command that produces a result.</summary>
    Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default);

    /// <summary>Dispatches a void command (returns <see cref="Unit"/>).</summary>
    Task SendAsync(ICommand command, CancellationToken cancellationToken = default);

    /// <summary>Dispatches a query and returns the result.</summary>
    Task<TResult> QueryAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Emits an event to all registered handlers.
    /// All handlers are invoked; exceptions are collected and re-thrown as <see cref="AggregateException"/>.
    /// </summary>
    Task EmitAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent;
}
