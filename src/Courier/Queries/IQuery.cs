namespace Courier.Queries;

/// <summary>
/// Marks a query that returns data of type <typeparamref name="TResult"/>.
/// Queries are read-only operations and must not produce side effects.
/// </summary>
/// <typeparam name="TResult">The type of data returned by the query.</typeparam>
public interface IQuery<out TResult> { }
