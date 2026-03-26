using Courier.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Courier.Tests;

/// <summary>
/// Base helper that builds a DI container with Courier registered.
/// Tests derive from this or use the <see cref="Build"/> factory method.
/// </summary>
internal static class DispatcherFixture
{
    public static IDispatcher Build(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddCourier(typeof(DispatcherFixture).Assembly);
        configure?.Invoke(services);

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IDispatcher>();
    }
}
