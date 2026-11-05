using BuildingBlocks.Exceptions;
using Inventory.Features.Inventory.AdjustStock;
using Inventory.Features.Inventory.SetStock;
using Inventory.IntegrationTests.Infrastructure;

namespace Inventory.IntegrationTests.Features;

public class SetStockTests(PostgresFixture db) : InventoryTestBase(db)
{
    private async Task SetAsync(Guid productId, int quantity, string reason = "recount")
    {
        await using var context = NewContext();
        await new SetStockCommandHandler(context)
            .Handle(new SetStockCommand(productId, quantity, reason), CancellationToken.None);
    }

    [Fact]
    public async Task ReplacesQuantityOnHand()
    {
        var productId = await SeedItemAsync(onHand: 10);

        await SetAsync(productId, 3);

        Assert.Equal(3, (await LoadAsync(productId))!.QuantityOnHand);
    }

    [Fact]
    public async Task UnknownProduct_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => SetAsync(Guid.NewGuid(), 5));
    }

    [Fact]
    public async Task DeletedProduct_ThrowsNotFound_AndIsNotChanged()
    {
        var productId = await SeedItemAsync(onHand: 10, deleted: true);

        await Assert.ThrowsAsync<NotFoundException>(() => SetAsync(productId, 5));
        Assert.Equal(10, (await LoadAsync(productId))!.QuantityOnHand);
    }

    [Fact]
    public async Task BelowReserved_ThrowsConflict_AndIsNotSaved()
    {
        var productId = await SeedItemAsync(onHand: 10, reserved: 6);

        await Assert.ThrowsAsync<ConflictException>(() => SetAsync(productId, 5));
        Assert.Equal(10, (await LoadAsync(productId))!.QuantityOnHand);
    }
}

public class AdjustStockTests(PostgresFixture db) : InventoryTestBase(db)
{
    private async Task AdjustAsync(Guid productId, int change, string reason = "recount")
    {
        await using var context = NewContext();
        await new AdjustStockCommandHandler(context)
            .Handle(new AdjustStockCommand(productId, change, reason), CancellationToken.None);
    }

    [Theory]
    [InlineData(5, 15)]
    [InlineData(-10, 0)]
    public async Task AppliesChange(int change, int expected)
    {
        var productId = await SeedItemAsync(onHand: 10);

        await AdjustAsync(productId, change);

        Assert.Equal(expected, (await LoadAsync(productId))!.QuantityOnHand);
    }

    [Fact]
    public async Task SequentialAdjustments_Accumulate()
    {
        var productId = await SeedItemAsync(onHand: 10);

        await AdjustAsync(productId, 5);
        await AdjustAsync(productId, -3);

        Assert.Equal(12, (await LoadAsync(productId))!.QuantityOnHand);
    }

    [Fact]
    public async Task BelowZero_ThrowsConflict_AndIsNotSaved()
    {
        var productId = await SeedItemAsync(onHand: 2);

        await Assert.ThrowsAsync<ConflictException>(() => AdjustAsync(productId, -3));
        Assert.Equal(2, (await LoadAsync(productId))!.QuantityOnHand);
    }

    [Fact]
    public async Task BelowReserved_ThrowsConflict()
    {
        var productId = await SeedItemAsync(onHand: 10, reserved: 8);

        await Assert.ThrowsAsync<ConflictException>(() => AdjustAsync(productId, -3));
    }

    [Fact]
    public async Task UnknownProduct_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => AdjustAsync(Guid.NewGuid(), 1));
    }

    [Fact]
    public async Task DeletedProduct_ThrowsNotFound()
    {
        var productId = await SeedItemAsync(onHand: 10, deleted: true);

        await Assert.ThrowsAsync<NotFoundException>(() => AdjustAsync(productId, 1));
    }
}
