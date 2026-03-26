using Courier.Commands;
using Courier.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace Courier.Tests;

public class PipelineTests
{
    // ── Fakes ──────────────────────────────────────────────────────────────

    private record PingCommand(string Message) : ICommand<string>;

    private class PingHandler : ICommandHandler<PingCommand, string>
    {
        public Task<string> HandleAsync(PingCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult($"pong:{command.Message}");
    }

    private class UpperCaseStep : IPipelineStep<PingCommand, string>
    {
        public static readonly List<string> Log = [];

        public async Task<string> ExecuteAsync(PingCommand input, Func<Task<string>> next, CancellationToken cancellationToken)
        {
            Log.Add("before");
            var result = await next();
            Log.Add("after");
            return result.ToUpperInvariant();
        }
    }

    private class PrefixStep : IPipelineStep<PingCommand, string>
    {
        public async Task<string> ExecuteAsync(PingCommand input, Func<Task<string>> next, CancellationToken cancellationToken)
        {
            var result = await next();
            return $"[prefixed] {result}";
        }
    }

    private class ShortCircuitStep : IPipelineStep<PingCommand, string>
    {
        public Task<string> ExecuteAsync(PingCommand input, Func<Task<string>> next, CancellationToken cancellationToken)
            => Task.FromResult("short-circuited");
    }

    // ── Tests ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Pipeline_SingleStep_TransformsResult()
    {
        // Arrange
        UpperCaseStep.Log.Clear();
        var dispatcher = DispatcherFixture.Build(services =>
        {
            services.AddScoped<IPipelineStep<PingCommand, string>, UpperCaseStep>();
        });

        // Act
        var result = await dispatcher.SendAsync(new PingCommand("hello"));

        // Assert
        Assert.Equal("PONG:HELLO", result);
        Assert.Equal(["before", "after"], UpperCaseStep.Log);
    }

    [Fact]
    public async Task Pipeline_MultipleSteps_ExecuteOuterFirst()
    {
        // Arrange — PrefixStep wraps UpperCaseStep wraps handler
        var dispatcher = DispatcherFixture.Build(services =>
        {
            services.AddScoped<IPipelineStep<PingCommand, string>, UpperCaseStep>();
            services.AddScoped<IPipelineStep<PingCommand, string>, PrefixStep>();
        });

        // Act
        var result = await dispatcher.SendAsync(new PingCommand("world"));

        // Assert: UpperCaseStep (registered first) is outermost, PrefixStep is inner.
        // chain: UpperCase( Prefix( handler ) ) → "[PREFIXED] PONG:WORLD"
        Assert.StartsWith("[PREFIXED]", result);
        Assert.Contains("PONG:WORLD", result);
    }

    [Fact]
    public async Task Pipeline_ShortCircuitStep_HandlerNotCalled()
    {
        // Arrange
        var dispatcher = DispatcherFixture.Build(services =>
        {
            services.AddScoped<IPipelineStep<PingCommand, string>, ShortCircuitStep>();
        });

        // Act
        var result = await dispatcher.SendAsync(new PingCommand("ignored"));

        // Assert
        Assert.Equal("short-circuited", result);
    }

    [Fact]
    public async Task Pipeline_NoSteps_CallsHandlerDirectly()
    {
        // Arrange — no additional steps, only handler from auto-scan
        var dispatcher = DispatcherFixture.Build();

        // Act
        var result = await dispatcher.SendAsync(new PingCommand("direct"));

        // Assert
        Assert.Equal("pong:direct", result);
    }
}
