using Courier.DependencyInjection;
using Courier.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Courier.Tests;

public class QueryHandlingTests
{
    // ── Fakes ──────────────────────────────────────────────────────────────

    private record GetOrderQuery(int OrderId) : IQuery<OrderDto>;
    private record OrderDto(int Id, string Product);

    private class GetOrderHandler : IQueryHandler<GetOrderQuery, OrderDto>
    {
        public Task<OrderDto> HandleAsync(GetOrderQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult(new OrderDto(query.OrderId, "Widget"));
    }

    private record ListOrdersQuery(int Page) : IQuery<IReadOnlyList<OrderDto>>;

    private class ListOrdersHandler : IQueryHandler<ListOrdersQuery, IReadOnlyList<OrderDto>>
    {
        public Task<IReadOnlyList<OrderDto>> HandleAsync(ListOrdersQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<OrderDto>>(
                [new OrderDto(1, "Alpha"), new OrderDto(2, "Beta")]);
    }

    // ── Tests ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task QueryAsync_ReturnsHandlerResult()
    {
        // Arrange
        var dispatcher = DispatcherFixture.Build();

        // Act
        var result = await dispatcher.QueryAsync(new GetOrderQuery(7));

        // Assert
        Assert.Equal(7, result.Id);
        Assert.Equal("Widget", result.Product);
    }

    [Fact]
    public async Task QueryAsync_ListResult_ReturnsList()
    {
        // Arrange
        var dispatcher = DispatcherFixture.Build();

        // Act
        var result = await dispatcher.QueryAsync(new ListOrdersQuery(1));

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task QueryAsync_NullQuery_ThrowsArgumentNullException()
    {
        var dispatcher = DispatcherFixture.Build();
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            dispatcher.QueryAsync<OrderDto>(null!));
    }

    [Fact]
    public async Task QueryAsync_NoHandlerRegistered_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        services.AddCourier(typeof(DispatcherFixture).Assembly);
        var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IDispatcher>();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dispatcher.QueryAsync(new UnregisteredQuery()));
    }

    // No handler registered for this query
    private record UnregisteredQuery : IQuery<string>;
}
