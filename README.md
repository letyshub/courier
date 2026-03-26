# Courier

[![CI](https://github.com/letyshub/simple-dotnet-cqrs/actions/workflows/ci.yml/badge.svg)](https://github.com/letyshub/simple-dotnet-cqrs/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Courier.CQRS.svg)](https://www.nuget.org/packages/Courier.CQRS)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

**Courier** is a lightweight CQRS library for .NET 8+ that provides a clean way to dispatch **commands**, **queries**, and **events** through a typed pipeline — with no magic, no reflection overhead at startup, and full DI support.

## Features

- Separate `ICommand<TResult>` and `IQuery<TResult>` interfaces (commands mutate state, queries read data)
- Domain `IEvent` with fan-out to multiple handlers
- Pipeline steps (`IPipelineStep<TInput, TOutput>`) for cross-cutting concerns (logging, validation, caching…)
- Auto-scan handlers from assemblies via `AddCourier()`
- Zero configuration required — works out of the box with `Microsoft.Extensions.DependencyInjection`

## Installation

```bash
dotnet add package Courier.CQRS
```

## Quick Start

### 1. Define your messages

```csharp
using Courier.Commands;
using Courier.Queries;
using Courier.Events;

// A command that returns the new product's id
record CreateProductCommand(string Name, decimal Price) : ICommand<int>;

// A void command (no return value)
record DeleteProductCommand(int ProductId) : ICommand;

// A query
record GetProductQuery(int ProductId) : IQuery<ProductDto>;

// A domain event
record ProductCreatedEvent(int ProductId, string Name) : IEvent;
```

### 2. Implement handlers

```csharp
using Courier.Commands;

class CreateProductHandler : ICommandHandler<CreateProductCommand, int>
{
    private readonly AppDbContext _db;

    public CreateProductHandler(AppDbContext db) => _db = db;

    public async Task<int> HandleAsync(CreateProductCommand command, CancellationToken ct = default)
    {
        var product = new Product(command.Name, command.Price);
        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);
        return product.Id;
    }
}

class DeleteProductHandler : ICommandHandler<DeleteProductCommand>
{
    public Task<Unit> HandleAsync(DeleteProductCommand command, CancellationToken ct = default)
    {
        // fire-and-forget style removal
        return Unit.Task;
    }
}
```

```csharp
using Courier.Queries;

class GetProductHandler : IQueryHandler<GetProductQuery, ProductDto>
{
    public Task<ProductDto> HandleAsync(GetProductQuery query, CancellationToken ct = default)
        => Task.FromResult(new ProductDto(query.ProductId, "Widget", 9.99m));
}
```

```csharp
using Courier.Events;

class ProductCreatedEmailHandler : IEventHandler<ProductCreatedEvent>
{
    public Task HandleAsync(ProductCreatedEvent @event, CancellationToken ct = default)
    {
        Console.WriteLine($"Sending welcome email for product {@event.Name}");
        return Task.CompletedTask;
    }
}
```

### 3. Register with DI

```csharp
// Program.cs / Startup.cs
builder.Services.AddCourier(typeof(Program).Assembly);
```

This automatically registers all `ICommandHandler<,>`, `IQueryHandler<,>`, and `IEventHandler<>` found in the given assemblies.

### 4. Dispatch

```csharp
public class ProductsController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public ProductsController(IDispatcher dispatcher) => _dispatcher = dispatcher;

    [HttpPost]
    public async Task<IActionResult> Create(CreateProductRequest req)
    {
        var id = await _dispatcher.SendAsync(new CreateProductCommand(req.Name, req.Price));
        return CreatedAtAction(nameof(Get), new { id }, null);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var dto = await _dispatcher.QueryAsync(new GetProductQuery(id));
        return Ok(dto);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _dispatcher.SendAsync(new DeleteProductCommand(id));
        return NoContent();
    }
}
```

---

## Pipeline Steps

Pipeline steps wrap command/query execution — great for logging, validation, retries, or performance monitoring.

```csharp
using Courier.Pipeline;

class ValidationStep<TInput, TOutput> : IPipelineStep<TInput, TOutput>
    where TInput : IValidatable
{
    public async Task<TOutput> ExecuteAsync(TInput input, Func<Task<TOutput>> next, CancellationToken ct)
    {
        input.Validate(); // throws ValidationException on failure
        return await next();
    }
}
```

Register steps **explicitly** (they are intentionally not auto-scanned):

```csharp
// Applies only to CreateProductCommand
builder.Services.AddScoped<
    IPipelineStep<CreateProductCommand, int>,
    ValidationStep<CreateProductCommand, int>>();
```

Steps execute in registration order (first registered = outermost wrapper).

---

## Events

Multiple handlers can react to the same event. All handlers are always invoked; any exceptions are collected into a single `AggregateException`.

```csharp
// Emitting an event (e.g. after saving to DB)
await _dispatcher.EmitAsync(new ProductCreatedEvent(product.Id, product.Name));
```

```csharp
// Second handler for the same event
class ProductCreatedAuditHandler : IEventHandler<ProductCreatedEvent>
{
    public Task HandleAsync(ProductCreatedEvent @event, CancellationToken ct = default)
    {
        Console.WriteLine($"Audit: product {@event.ProductId} created");
        return Task.CompletedTask;
    }
}
```

---

## API Reference

| Type | Description |
|------|-------------|
| `ICommand<TResult>` | Marks a state-changing operation returning `TResult` |
| `ICommand` | Shorthand for `ICommand<Unit>` (no return value) |
| `IQuery<TResult>` | Marks a read-only operation returning `TResult` |
| `IEvent` | Marks a domain event (fan-out to multiple handlers) |
| `ICommandHandler<TCommand, TResult>` | Handles a command |
| `ICommandHandler<TCommand>` | Handles a void command |
| `IQueryHandler<TQuery, TResult>` | Handles a query |
| `IEventHandler<TEvent>` | Handles an event |
| `IPipelineStep<TInput, TOutput>` | Cross-cutting pipeline middleware |
| `IDispatcher` | Central entry point for dispatching |
| `Unit` | The empty return type for void commands |

---

## Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/my-feature`)
3. Write tests for your changes
4. Open a pull request

See [CONTRIBUTING.md](CONTRIBUTING.md) for full guidelines.

## License

[MIT](LICENSE)
