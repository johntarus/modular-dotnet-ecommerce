using BuildingBlocks.Exceptions;
using BuildingBlocks.Shared;
using Inventory.Features.Inventory.GetInventory;
using Inventory.Features.Inventory.GetInventoryById;
using Inventory.IntegrationTests.Infrastructure;

namespace Inventory.IntegrationTests.Features;

public class GetInventoryByIdTests(PostgresFixture db) : InventoryTestBase(db)
{
    private async Task<Inventory.Features.Inventory.GetInventoryById.InventoryResponse> GetAsync(Guid productId)
    {
        await using var context = NewContext();
        return await new GetInventoryByIdQueryHandler(context)
            .Handle(new GetInventoryByIdQuery(productId), CancellationToken.None);
    }

    [Fact]
    public async Task ReturnsQuantities()
    {
        var productId = await SeedItemAsync(onHand: 10, reserved: 3);

        var response = await GetAsync(productId);

        Assert.Equal(productId, response.ProductId);
        Assert.Equal(10, response.QuantityOnHand);
        Assert.Equal(3, response.QuantityReserved);
        Assert.Equal(7, response.AvailableQuantity);
    }

    [Fact]
    public async Task UnknownProduct_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeletedProduct_ThrowsNotFound()
    {
        var productId = await SeedItemAsync(onHand: 10, deleted: true);

        await Assert.ThrowsAsync<NotFoundException>(() => GetAsync(productId));
    }
}

public class GetInventoryTests(PostgresFixture db) : InventoryTestBase(db)
{
    private async Task<PagedResult<Inventory.Features.Inventory.GetInventory.InventoryResponse>> GetAsync(int page, int pageSize)
    {
        await using var context = NewContext();
        return await new GetInventoryQueryHandler(context)
            .Handle(new GetInventoryQuery(page, pageSize), CancellationToken.None);
    }

    [Fact]
    public async Task PagesThroughItems()
    {
        for (var i = 0; i < 5; i++) await SeedItemAsync(onHand: i + 1);

        var first = await GetAsync(page: 1, pageSize: 2);
        var last = await GetAsync(page: 3, pageSize: 2);

        Assert.Equal(5, first.TotalCount);
        Assert.Equal(3, first.TotalPages);
        Assert.Equal(2, first.Items.Count);
        Assert.False(first.HasPreviousPage);
        Assert.True(first.HasNextPage);
        Assert.Single(last.Items);
        Assert.True(last.HasPreviousPage);
        Assert.False(last.HasNextPage);
    }

    [Fact]
    public async Task PagesDoNotOverlap()
    {
        for (var i = 0; i < 4; i++) await SeedItemAsync();

        var first = await GetAsync(1, 2);
        var second = await GetAsync(2, 2);

        Assert.Empty(first.Items.Select(i => i.ProductId).Intersect(second.Items.Select(i => i.ProductId)));
    }

    [Fact]
    public async Task ExcludesDeletedItems()
    {
        var active = await SeedItemAsync(onHand: 1);
        await SeedItemAsync(onHand: 1, deleted: true);

        var result = await GetAsync(1, 10);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(active, Assert.Single(result.Items).ProductId);
    }

    [Fact]
    public async Task PageSizeIsClampedToMaximum()
    {
        var result = await GetAsync(1, 10_000);

        Assert.Equal(PaginationRequest.MaxPageSize, result.PageSize);
    }

    [Fact]
    public async Task Empty_ReturnsNoPages()
    {
        var result = await GetAsync(1, 10);

        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
        Assert.Empty(result.Items);
    }
}
