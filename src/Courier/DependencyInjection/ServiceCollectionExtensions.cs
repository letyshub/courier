using System.Reflection;
using Courier.Commands;
using Courier.Events;
using Courier.Pipeline;
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

        foreach (var assembly in assemblies)
            RegisterHandlersFromAssembly(services, assembly);

        return new CourierBuilder(services);
    }

    /// <summary>
    /// Registers the <see cref="IDispatcher"/> and scans the assembly containing <typeparamref name="TMarker"/>.
    /// </summary>
    public static CourierBuilder AddCourier<TMarker>(this IServiceCollection services)
        => services.AddCourier(typeof(TMarker).Assembly);

    // ---

    private static void RegisterHandlersFromAssembly(IServiceCollection services, Assembly assembly)
    {
        var types = assembly.GetTypes().Where(t => t is { IsAbstract: false, IsInterface: false });

        foreach (var type in types)
        {
            RegisterCommandHandlers(services, type);
            RegisterQueryHandlers(services, type);
            RegisterEventHandlers(services, type);
            // Pipeline steps are NOT auto-scanned — register them explicitly via services.AddScoped<IPipelineStep<,>,Impl>()
        }
    }

    private static void RegisterCommandHandlers(IServiceCollection services, Type type)
    {
        foreach (var iface in type.GetInterfaces())
        {
            if (!iface.IsGenericType) continue;

            var def = iface.GetGenericTypeDefinition();
            if (def == typeof(ICommandHandler<,>) || def == typeof(ICommandHandler<>))
                services.TryAddScoped(iface, type);
        }
    }

    private static void RegisterQueryHandlers(IServiceCollection services, Type type)
    {
        foreach (var iface in type.GetInterfaces())
        {
            if (!iface.IsGenericType) continue;

            var def = iface.GetGenericTypeDefinition();
            if (def == typeof(IQueryHandler<,>))
                services.TryAddScoped(iface, type);
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
