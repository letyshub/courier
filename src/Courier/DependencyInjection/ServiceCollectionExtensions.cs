using System.Reflection;
using Courier.Commands;
using Courier.Events;
using Courier.Queries;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Courier.DependencyInjection;

/// <summary>
/// Extension methods for registering Courier services with the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="IDispatcher"/> and scans the provided assemblies for handlers.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">Assemblies to scan for command, query and event handlers.</param>
    /// <returns>A <see cref="CourierBuilder"/> for further configuration.</returns>
    public static CourierBuilder AddCourier(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.TryAddScoped<IDispatcher, Dispatcher>();

        var builder = new CourierBuilder(services);

        foreach (var assembly in assemblies)
            RegisterHandlersFromAssembly(builder, assembly);

        return builder;
    }

    /// <summary>
    /// Registers the <see cref="IDispatcher"/> and scans the assembly containing <typeparamref name="TMarker"/>.
    /// </summary>
    public static CourierBuilder AddCourier<TMarker>(this IServiceCollection services)
        => services.AddCourier(typeof(TMarker).Assembly);

    // ---

    private static void RegisterHandlersFromAssembly(CourierBuilder builder, Assembly assembly)
    {
        var concreteTypes = assembly.GetTypes().Where(t => t is { IsAbstract: false, IsInterface: false });

        foreach (var type in concreteTypes)
        {
            RegisterCommandHandlers(builder, type);
            RegisterQueryHandlers(builder, type);
            RegisterEventHandlers(builder.Services, type);
            // Pipeline steps are NOT auto-scanned — use builder.AddPipelineStep() or register explicitly.
        }
    }

    private static void RegisterCommandHandlers(CourierBuilder builder, Type type)
    {
        foreach (var iface in type.GetInterfaces())
        {
            if (!iface.IsGenericType) continue;

            var def = iface.GetGenericTypeDefinition();

            if (def == typeof(ICommandHandler<,>))
            {
                builder.Services.TryAddScoped(iface, type);
                // Record (TCommand, TResult) for open-generic pipeline step registration.
                var args = iface.GetGenericArguments(); // [TCommand, TResult]
                builder.DiscoveredPipelines.Add((args[0], args[1]));
            }
            else if (def == typeof(ICommandHandler<>))
            {
                builder.Services.TryAddScoped(iface, type);
                // ICommandHandler<TCommand> → TResult = Unit
                var commandType = iface.GetGenericArguments()[0];
                builder.DiscoveredPipelines.Add((commandType, typeof(Unit)));
            }
        }
    }

    private static void RegisterQueryHandlers(CourierBuilder builder, Type type)
    {
        foreach (var iface in type.GetInterfaces())
        {
            if (!iface.IsGenericType) continue;

            if (iface.GetGenericTypeDefinition() == typeof(IQueryHandler<,>))
            {
                builder.Services.TryAddScoped(iface, type);
                // Record (TQuery, TResult) for open-generic pipeline step registration.
                var args = iface.GetGenericArguments(); // [TQuery, TResult]
                builder.DiscoveredPipelines.Add((args[0], args[1]));
            }
        }
    }

    private static void RegisterEventHandlers(IServiceCollection services, Type type)
    {
        foreach (var iface in type.GetInterfaces())
        {
            if (!iface.IsGenericType) continue;

            if (iface.GetGenericTypeDefinition() == typeof(IEventHandler<>))
                services.AddScoped(iface, type); // multiple handlers per event are allowed
        }
    }
}
