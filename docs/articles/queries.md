# Queries

Queries represent **read-only operations** — they return data and must not produce side effects.

## Defining a Query

Implement `IQuery<TResult>`:

```csharp
using Courier.Queries;

record GetProductQuery(int ProductId) : IQuery<ProductDto>;
record ListProductsQuery(int Page, int PageSize) : IQuery<PagedResult<ProductDto>>;
```

## Implementing a Handler

```csharp
class GetProductHandler : IQueryHandler<GetProductQuery, ProductDto>
{
    private readonly AppDbContext _db;

    public GetProductHandler(AppDbContext db) => _db = db;

    public async Task<ProductDto> HandleAsync(GetProductQuery query, CancellationToken ct = default)
    {
        var product = await _db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == query.ProductId, ct)
            ?? throw new NotFoundException($"Product {query.ProductId} not found.");

        return new ProductDto(product.Id, product.Name, product.Price);
    }
}
```

## Dispatching

```csharp
var dto  = await _dispatcher.QueryAsync(new GetProductQuery(42));
var page = await _dispatcher.QueryAsync(new ListProductsQuery(Page: 1, PageSize: 20));
```

## Rules

- Exactly **one handler** per query type. If none is found an `InvalidOperationException` is thrown.
- Queries **must not mutate** state — use commands for state changes.
- Always pass the `CancellationToken` through to database and I/O calls.
