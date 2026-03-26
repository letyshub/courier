using Courier.Pipeline;
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

    /// <summary>
    /// All (InputType, OutputType) pairs discovered during assembly scanning.
    /// Used by <see cref="CourierBuilderExtensions.AddPipelineStep"/> to close open-generic step types.
    /// </summary>
    internal List<(Type InputType, Type OutputType)> DiscoveredPipelines { get; } = [];
}

/// <summary>
/// Extension methods on <see cref="CourierBuilder"/> for pipeline step registration.
/// </summary>
public static class CourierBuilderExtensions
{
    /// <summary>
    /// Registers an open-generic pipeline step so it applies to every command and query
    /// discovered by <c>AddCourier()</c>.
    /// </summary>
    /// <param name="builder">The builder returned by <c>AddCourier()</c>.</param>
    /// <param name="openGenericStepType">
    /// An open-generic type with exactly two type parameters that implements
    /// <see cref="IPipelineStep{TInput,TOutput}"/>, e.g. <c>typeof(LoggingStep&lt;,&gt;)</c>.
    /// </param>
    /// <returns>The same builder for chaining.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="openGenericStepType"/> is not an open generic type definition
    /// with exactly two type parameters.
    /// </exception>
    public static CourierBuilder AddPipelineStep(this CourierBuilder builder, Type openGenericStepType)
    {
        ArgumentNullException.ThrowIfNull(openGenericStepType);

        if (!openGenericStepType.IsGenericTypeDefinition)
            throw new ArgumentException(
                $"'{openGenericStepType.Name}' must be an open-generic type definition (e.g. typeof(MyStep<,>)).",
                nameof(openGenericStepType));

        if (openGenericStepType.GetGenericArguments().Length != 2)
            throw new ArgumentException(
                $"'{openGenericStepType.Name}' must have exactly two generic type parameters " +
                $"(matching IPipelineStep<TInput, TOutput>).",
                nameof(openGenericStepType));

        foreach (var (inputType, outputType) in builder.DiscoveredPipelines)
        {
            try
            {
                var closedStep = openGenericStepType.MakeGenericType(inputType, outputType);
                var closedInterface = typeof(IPipelineStep<,>).MakeGenericType(inputType, outputType);
                builder.Services.AddScoped(closedInterface, closedStep);
            }
            catch (ArgumentException)
            {
                // Generic constraints on the step type may exclude certain input/output pairs — skip those.
            }
        }

        return builder;
    }
}
