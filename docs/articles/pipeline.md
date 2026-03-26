# Pipeline Steps

Pipeline steps are middleware that wrap command and query execution. Use them for cross-cutting concerns: logging, validation, caching, retry, performance monitoring, etc.

## Defining a Step

Implement `IPipelineStep<TInput, TOutput>`:

```csharp
using Courier.Pipeline;

class LoggingStep<TInput, TOutput> : IPipelineStep<TInput, TOutput>
{
    private readonly ILogger<LoggingStep<TInput, TOutput>> _logger;

    public LoggingStep(ILogger<LoggingStep<TInput, TOutput>> logger) => _logger = logger;

    public async Task<TOutput> ExecuteAsync(TInput input, Func<Task<TOutput>> next, CancellationToken ct)
    {
        _logger.LogInformation("Handling {Type}", typeof(TInput).Name);
        var result = await next();
        _logger.LogInformation("Handled  {Type}", typeof(TInput).Name);
        return result;
    }
}
```

Calling `next()` invokes the rest of the pipeline (or the handler itself). You can short-circuit by returning without calling `next()`.

## Registering Steps

### Per-command/query (closed generic)

```csharp
builder.Services.AddScoped<
    IPipelineStep<CreateProductCommand, int>,
    LoggingStep<CreateProductCommand, int>>();
```

### For all discovered commands and queries (open generic)

Use `AddPipelineStep` on the `CourierBuilder` returned by `AddCourier`:

```csharp
builder.Services
    .AddCourier(typeof(Program).Assembly)
    .AddPipelineStep(typeof(LoggingStep<,>));
```

This automatically registers the step against every command and query type found during assembly scanning.

## Execution Order

Steps execute in registration order, outermost first:

```
First registered step → Second registered step → … → Handler
```

## Example: Validation Step

```csharp
class ValidationStep<TInput, TOutput> : IPipelineStep<TInput, TOutput>
{
    private readonly IEnumerable<IValidator<TInput>> _validators;

    public ValidationStep(IEnumerable<IValidator<TInput>> validators)
        => _validators = validators;

    public async Task<TOutput> ExecuteAsync(TInput input, Func<Task<TOutput>> next, CancellationToken ct)
    {
        foreach (var validator in _validators)
            await validator.ValidateAndThrowAsync(input, ct);

        return await next();
    }
}
```

## Example: Timing Step

```csharp
class TimingStep<TInput, TOutput> : IPipelineStep<TInput, TOutput>
{
    private readonly ILogger<TimingStep<TInput, TOutput>> _logger;

    public TimingStep(ILogger<TimingStep<TInput, TOutput>> logger) => _logger = logger;

    public async Task<TOutput> ExecuteAsync(TInput input, Func<Task<TOutput>> next, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            return await next();
        }
        finally
        {
            _logger.LogDebug("{Type} completed in {Ms}ms", typeof(TInput).Name, sw.ElapsedMilliseconds);
        }
    }
}
```
