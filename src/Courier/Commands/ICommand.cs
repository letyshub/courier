namespace Courier.Commands;

/// <summary>
/// Marks a command that produces a result of type <typeparamref name="TResult"/>.
/// Commands represent state-changing intentions; they return a result upon completion.
/// </summary>
/// <typeparam name="TResult">The type of value produced after executing the command.</typeparam>
public interface ICommand<out TResult> { }

/// <summary>
/// Marks a command that produces no meaningful result.
/// Shorthand for <see cref="ICommand{TResult}"/> where <c>TResult</c> is <see cref="Unit"/>.
/// </summary>
public interface ICommand : ICommand<Unit> { }
