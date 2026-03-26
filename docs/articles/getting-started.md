# Getting Started

## Installation

```bash
dotnet add package Courier.CQRS
```

## Registration

Call `AddCourier()` in your `Program.cs` or `Startup.cs`, passing the assemblies that contain your handlers:

```csharp
builder.Services.AddCourier(typeof(Program).Assembly);
```

This automatically registers all `ICommandHandler<,>`, `IQueryHandler<,>`, and `IEventHandler<>` found in the given assemblies.
You can pass multiple assemblies:

```csharp
builder.Services.AddCourier(
    typeof(Program).Assembly,
    typeof(MyHandlers).Assembly);
```

Or use the marker-type overload:

```csharp
builder.Services.AddCourier<MyHandlerMarker>();
```

## Injecting the Dispatcher

`IDispatcher` is registered as a scoped service. Inject it wherever you need to dispatch:

```csharp
public class OrdersController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public OrdersController(IDispatcher dispatcher)
        => _dispatcher = dispatcher;
}
```

## Next Steps

- [Commands](commands.md) — learn how to define state-changing operations
- [Queries](queries.md) — learn how to define read-only operations
- [Events](events.md) — publish domain events to multiple subscribers
- [Pipeline Steps](pipeline.md) — add cross-cutting concerns (logging, validation, …)
