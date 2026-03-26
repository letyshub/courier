namespace Courier.Pipeline;

/// <summary>
/// A cross-cutting step that wraps the execution of a command or query handler.
/// Steps are executed in registration order, innermost-first (last registered = closest to handler).
/// </summary>
/// <typeparam name="TInput">The command or query type.</typeparam>
/// <typeparam name="TOutput">The result type produced by the handler.</typeparam>
public interface IPipelineStep<TInput, TOutput>
{
    /// <summary>
    /// Processes the input, optionally short-circuiting by not calling <paramref name="next"/>.
    /// </summary>
    /// <param name="input">The command or query being dispatched.</param>
    /// <param name="next">Delegate that continues to the next step or the actual handler.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    Task<TOutput> ExecuteAsync(TInput input, Func<Task<TOutput>> next, CancellationToken cancellationToken);
}
