---
_layout: landing
---

# Courier CQRS

A lightweight CQRS mediator library for .NET 8+ — dispatch commands, queries and events through a clean pipeline.

[![CI](https://github.com/letyshub/courier/actions/workflows/ci.yml/badge.svg)](https://github.com/letyshub/courier/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Courier.CQRS.svg)](https://www.nuget.org/packages/Courier.CQRS)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/letyshub/courier/blob/master/LICENSE)

## Why Courier?

| Feature | Courier |
|---------|---------|
| Commands and queries are **separate types** | ✔ |
| Domain events with fan-out | ✔ |
| Typed pipeline middleware | ✔ |
| Open-generic step registration | ✔ |
| Zero-reflection startup cost | ✔ |
| Full DI integration | ✔ |

## Quick Install

```bash
dotnet add package Courier.CQRS
```

## Quick Start

```csharp
// 1. Define messages
record CreateOrderCommand(string Product) : ICommand<int>;
record GetOrderQuery(int Id)              : IQuery<OrderDto>;
record OrderCreatedEvent(int Id)          : IEvent;

// 2. Register
builder.Services.AddCourier(typeof(Program).Assembly);

// 3. Dispatch
var id  = await dispatcher.SendAsync(new CreateOrderCommand("Widget"));
var dto = await dispatcher.QueryAsync(new GetOrderQuery(id));
await dispatcher.EmitAsync(new OrderCreatedEvent(id));
```

## Documentation

- [Getting Started](articles/getting-started.md)
- [Commands](articles/commands.md)
- [Queries](articles/queries.md)
- [Events](articles/events.md)
- [Pipeline Steps](articles/pipeline.md)
- [API Reference](api/index.md)
