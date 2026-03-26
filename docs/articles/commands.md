# Commands

Commands represent **state-changing intentions** — create, update, delete. They always return a result (or `Unit` when no value is needed).

## Defining a Command

Implement `ICommand<TResult>` for commands that return a value:

```csharp
using Courier.Commands;

record CreateProductCommand(string Name, decimal Price) : ICommand<int>;
```

Use the shorthand `ICommand` (equivalent to `ICommand<Unit>`) when no return value is needed:

```csharp
record DeleteProductCommand(int ProductId) : ICommand;
```

## Implementing a Handler

```csharp
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
```

For void commands, implement `ICommandHandler<TCommand>`:

```csharp
class DeleteProductHandler : ICommandHandler<DeleteProductCommand>
{
    private readonly AppDbContext _db;

    public DeleteProductHandler(AppDbContext db) => _db = db;

    public async Task<Unit> HandleAsync(DeleteProductCommand command, CancellationToken ct = default)
    {
        var product = await _db.Products.FindAsync([command.ProductId], ct);
        if (product is not null)
        {
            _db.Products.Remove(product);
            await _db.SaveChangesAsync(ct);
        }
        return Unit.Value;
    }
}
```

## Dispatching

```csharp
// Command with result
int id = await _dispatcher.SendAsync(new CreateProductCommand("Widget", 9.99m));

// Void command
await _dispatcher.SendAsync(new DeleteProductCommand(id));
```

## Rules

- Exactly **one handler** per command type. If none is found a `InvalidOperationException` is thrown.
- Handlers are resolved from the DI container and are **scoped** by default.
- Always pass the `CancellationToken` to any async operations inside the handler.
