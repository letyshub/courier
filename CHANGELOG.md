# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2024-03-26

### Added
- `ICommand<TResult>` and `ICommand` interfaces for state-changing operations
- `IQuery<TResult>` interface for read-only operations
- `IEvent` interface for domain events with fan-out dispatch
- `ICommandHandler<,>`, `IQueryHandler<,>`, `IEventHandler<>` handler interfaces
- `IPipelineStep<TInput, TOutput>` for cross-cutting concerns
- `IDispatcher` with `SendAsync`, `QueryAsync`, and `EmitAsync`
- Assembly scanning via `AddCourier(Assembly[])` extension method
- `Unit` value type for void commands
- Full XML documentation on all public types
