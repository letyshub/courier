namespace Courier.Commands;

/// <summary>
/// Handles a command of type <typeparamref name="TCommand"/> and returns <typeparamref name="TResult"/>.
/// </summary>
/// <typeparam name="TCommand">The command type to handle.</typeparam>
/// <typeparam name="TResult">The result type produced by the handler.</typeparam>
public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    /// <summary>Executes the command and returns the result.</summary>
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Handles a void command that returns <see cref="Unit"/>.
/// Implement this instead of <see cref="ICommandHandler{TCommand,TResult}"/> when no result is needed.
/// </summary>
/// <typeparam name="TCommand">The command type to handle.</typeparam>
public interface ICommandHandler<in TCommand> : ICommandHandler<TCommand, Unit>
    where TCommand : ICommand<Unit>
{
}
