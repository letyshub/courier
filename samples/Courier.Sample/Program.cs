using Courier;
using Courier.Commands;
using Courier.DependencyInjection;
using Courier.Events;
using Courier.Pipeline;
using Courier.Queries;
using Microsoft.Extensions.DependencyInjection;

// ── Setup DI ──────────────────────────────────────────────────────────────
var services = new ServiceCollection();

services.AddCourier(typeof(Program).Assembly);

// Register a logging pipeline step for all CreateProductCommands
services.AddScoped<IPipelineStep<CreateProductCommand, int>, LoggingStep>();

var provider = services.BuildServiceProvider();
var dispatcher = provider.CreateScope().ServiceProvider.GetRequiredService<IDispatcher>();

// ── Commands ──────────────────────────────────────────────────────────────
Console.WriteLine("=== Commands ===");

var productId = await dispatcher.SendAsync(new CreateProductCommand("Laptop", 1299.99m));
Console.WriteLine($"Created product with id: {productId}");

await dispatcher.SendAsync(new ArchiveProductCommand(productId));
Console.WriteLine($"Archived product {productId}");

// ── Queries ───────────────────────────────────────────────────────────────
Console.WriteLine("\n=== Queries ===");

var product = await dispatcher.QueryAsync(new GetProductQuery(productId));
Console.WriteLine($"Fetched: {product.Name} @ {product.Price:C}");

var catalog = await dispatcher.QueryAsync(new ListProductsQuery());
Console.WriteLine($"Catalog has {catalog.Count} product(s)");

// ── Events ────────────────────────────────────────────────────────────────
Console.WriteLine("\n=== Events ===");

await dispatcher.EmitAsync(new ProductViewedEvent(productId, "user-42"));
Console.WriteLine("Event emitted");

// ==========================================================================
// Domain types
// ==========================================================================

record CreateProductCommand(string Name, decimal Price) : ICommand<int>;
record ArchiveProductCommand(int ProductId) : ICommand;
record GetProductQuery(int ProductId) : IQuery<ProductDto>;
record ListProductsQuery : IQuery<IReadOnlyList<ProductDto>>;
record ProductDto(int Id, string Name, decimal Price);
record ProductViewedEvent(int ProductId, string UserId) : IEvent;

// ==========================================================================
// Handlers
// ==========================================================================

class CreateProductHandler : ICommandHandler<CreateProductCommand, int>
{
    private static int _nextId = 1;

    public Task<int> HandleAsync(CreateProductCommand command, CancellationToken cancellationToken = default)
        => Task.FromResult(_nextId++);
}

class ArchiveProductHandler : ICommandHandler<ArchiveProductCommand>
{
    public Task<Unit> HandleAsync(ArchiveProductCommand command, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"  [handler] product {command.ProductId} archived");
        return Unit.Task;
    }
}

class GetProductHandler : IQueryHandler<GetProductQuery, ProductDto>
{
    public Task<ProductDto> HandleAsync(GetProductQuery query, CancellationToken cancellationToken = default)
        => Task.FromResult(new ProductDto(query.ProductId, "Laptop", 1299.99m));
}

class ListProductsHandler : IQueryHandler<ListProductsQuery, IReadOnlyList<ProductDto>>
{
    public Task<IReadOnlyList<ProductDto>> HandleAsync(ListProductsQuery query, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ProductDto>>([new ProductDto(1, "Laptop", 1299.99m)]);
}

class ProductViewedHandler : IEventHandler<ProductViewedEvent>
{
    public Task HandleAsync(ProductViewedEvent @event, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"  [event] user {@event.UserId} viewed product {@event.ProductId}");
        return Task.CompletedTask;
    }
}

// ==========================================================================
// Pipeline step
// ==========================================================================

class LoggingStep : IPipelineStep<CreateProductCommand, int>
{
    public async Task<int> ExecuteAsync(CreateProductCommand input, Func<Task<int>> next, CancellationToken cancellationToken)
    {
        Console.WriteLine($"  [pipeline] creating product '{input.Name}'...");
        var result = await next();
        Console.WriteLine($"  [pipeline] product created with id {result}");
        return result;
    }
}
