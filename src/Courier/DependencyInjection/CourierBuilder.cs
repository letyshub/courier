using Microsoft.Extensions.DependencyInjection;

namespace Courier.DependencyInjection;

/// <summary>
/// Fluent builder returned by <c>AddCourier()</c> for additional configuration.
/// </summary>
public sealed class CourierBuilder
{
    internal CourierBuilder(IServiceCollection services)
    {
        Services = services;
    }

    /// <summary>The underlying service collection.</summary>
    public IServiceCollection Services { get; }
}
