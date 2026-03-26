using Courier.Commands;
using Courier.DependencyInjection;
using Courier.Pipeline;
using Courier.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Courier.Tests;

public class OpenGenericPipelineTests
{
    // ── Fakes ──────────────────────────────────────────────────────────────

    private record EchoCommand(string Text) : ICommand<string>;
    private record EchoQuery(string Text) : IQuery<string>;
    private record VoidCommand(string Text) : ICommand;

    private class EchoCommandHandler : ICommandHandler<EchoCommand, string>
    {
        public Task<string> HandleAsync(EchoCommand command, CancellationToken ct = default)
            => Task.FromResult(command.Text);
    }

    private class EchoQueryHandler : IQueryHandler<EchoQuery, string>
    {
        public Task<string> HandleAsync(EchoQuery query, CancellationToken ct = default)
            => Task.FromResult(query.Text);
    }

    private class VoidCommandHandler : ICommandHandler<VoidCommand>
    {
        public Task<Unit> HandleAsync(VoidCommand command, CancellationToken ct = default)
            => Unit.Task;
    }

    /// <summary>Generic step that wraps the result in brackets — works for any TInput/TOutput.</summary>
    private class BracketStep<TInput, TOutput> : IPipelineStep<TInput, TOutput>
    {
        public static readonly List<string> Log = [];

        public async Task<TOutput> ExecuteAsync(TInput input, Func<Task<TOutput>> next, CancellationToken ct)
        {
            Log.Add($"before:{typeof(TInput).Name}");
            var result = await next();
            Log.Add($"after:{typeof(TInput).Name}");
            return result;
        }
    }

    // ── Tests ──────────────────────────────────────────────────────────────

    private static IDispatcher BuildWithOpenStep()
    {
        var services = new ServiceCollection();
        var builder = services.AddCourier(typeof(OpenGenericPipelineTests).Assembly);
        builder.AddPipelineStep(typeof(BracketStep<,>));

        var provider = services.BuildServiceProvider();
        return provider.CreateScope().ServiceProvider.GetRequiredService<IDispatcher>();
    }

    [Fact]
    public async Task AddPipelineStep_OpenGeneric_AppliesToCommand()
    {
        // Arrange
        BracketStep<EchoCommand, string>.Log.Clear();
        var dispatcher = BuildWithOpenStep();

        // Act
        var result = await dispatcher.SendAsync(new EchoCommand("hello"));

        // Assert — handler executed and step ran
        Assert.Equal("hello", result);
        Assert.Contains($"before:{nameof(EchoCommand)}", BracketStep<EchoCommand, string>.Log);
        Assert.Contains($"after:{nameof(EchoCommand)}", BracketStep<EchoCommand, string>.Log);
    }

    [Fact]
    public async Task AddPipelineStep_OpenGeneric_AppliesToQuery()
    {
        // Arrange
        BracketStep<EchoQuery, string>.Log.Clear();
        var dispatcher = BuildWithOpenStep();

        // Act
        var result = await dispatcher.QueryAsync(new EchoQuery("world"));

        // Assert
        Assert.Equal("world", result);
        Assert.Contains($"before:{nameof(EchoQuery)}", BracketStep<EchoQuery, string>.Log);
    }

    [Fact]
    public async Task AddPipelineStep_OpenGeneric_AppliesToVoidCommand()
    {
        // Arrange
        BracketStep<VoidCommand, Unit>.Log.Clear();
        var dispatcher = BuildWithOpenStep();

        // Act
        await dispatcher.SendAsync(new VoidCommand("fire-and-forget"));

        // Assert
        Assert.Contains($"before:{nameof(VoidCommand)}", BracketStep<VoidCommand, Unit>.Log);
    }

    [Fact]
    public void AddPipelineStep_NotOpenGeneric_ThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var builder = services.AddCourier();

        // Closed generic type — should be rejected
        Assert.Throws<ArgumentException>(() =>
            builder.AddPipelineStep(typeof(BracketStep<EchoCommand, string>)));
    }

    [Fact]
    public void AddPipelineStep_WrongNumberOfTypeParams_ThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var builder = services.AddCourier();

        Assert.Throws<ArgumentException>(() =>
            builder.AddPipelineStep(typeof(List<>))); // 1 type param, not 2
    }

    [Fact]
    public void AddPipelineStep_Null_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        var builder = services.AddCourier();

        Assert.Throws<ArgumentNullException>(() =>
            builder.AddPipelineStep(null!));
    }

    [Fact]
    public async Task AddPipelineStep_CalledTwice_BothStepsApply()
    {
        // Arrange — two different open-generic steps
        BracketStep<EchoCommand, string>.Log.Clear();

        var services = new ServiceCollection();
        var builder = services.AddCourier(typeof(OpenGenericPipelineTests).Assembly);
        builder.AddPipelineStep(typeof(BracketStep<,>));
        builder.AddPipelineStep(typeof(BracketStep<,>)); // register same step twice

        var provider = services.BuildServiceProvider();
        var dispatcher = provider.CreateScope().ServiceProvider.GetRequiredService<IDispatcher>();

        // Act
        await dispatcher.SendAsync(new EchoCommand("double"));

        // Assert — step ran twice (two registrations = two wraps)
        Assert.Equal(2, BracketStep<EchoCommand, string>.Log.Count(e => e.StartsWith("before:")));
    }
}
