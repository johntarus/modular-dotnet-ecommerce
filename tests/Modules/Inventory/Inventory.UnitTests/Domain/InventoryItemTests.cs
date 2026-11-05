using BuildingBlocks.Exceptions;
using Inventory.Domain.Entities;
using ValidationException = BuildingBlocks.Exceptions.ValidationException;

namespace Inventory.UnitTests.Domain;

public class InventoryItemTests
{
    private static readonly Guid ProductId = Guid.NewGuid();

    /// <summary>An item with the given on-hand and reserved quantities.</summary>
    private static InventoryItem Item(int onHand = 0, int reserved = 0)
    {
        var item = InventoryItem.Create(ProductId);
        if (onHand > 0) item.SetStock(onHand, "setup");
        if (reserved > 0) item.ReserveStock(reserved);
        return item;
    }

    // ---------- Create ----------

    [Fact]
    public void Create_StartsEmptyAndActive()
    {
        var item = InventoryItem.Create(ProductId);

        Assert.Equal(ProductId, item.ProductId);
        Assert.Equal(0, item.QuantityOnHand);
        Assert.Equal(0, item.QuantityReserved);
        Assert.Equal(0, item.AvailableQuantity);
        Assert.False(item.IsDeleted);
    }

    [Fact]
    public void AvailableQuantity_IsOnHandMinusReserved()
    {
        var item = Item(onHand: 10, reserved: 3);

        Assert.Equal(7, item.AvailableQuantity);
    }

    // ---------- AdjustStock ----------

    [Theory]
    [InlineData(5, 15)]
    [InlineData(-4, 6)]
    [InlineData(-10, 0)]
    public void AdjustStock_AppliesDelta(int delta, int expected)
    {
        var item = Item(onHand: 10);

        item.AdjustStock(delta, "recount");

        Assert.Equal(expected, item.QuantityOnHand);
    }

    [Fact]
    public void AdjustStock_ZeroDelta_Throws()
    {
        Assert.Throws<ValidationException>(() => Item(onHand: 10).AdjustStock(0, "recount"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AdjustStock_BlankReason_Throws(string reason)
    {
        Assert.Throws<ValidationException>(() => Item(onHand: 10).AdjustStock(1, reason));
    }

    [Fact]
    public void AdjustStock_BelowZero_ThrowsConflict_AndLeavesStockUnchanged()
    {
        var item = Item(onHand: 5);

        Assert.Throws<ConflictException>(() => item.AdjustStock(-6, "damaged"));
        Assert.Equal(5, item.QuantityOnHand);
    }

    [Fact]
    public void AdjustStock_BelowReserved_ThrowsConflict()
    {
        var item = Item(onHand: 10, reserved: 4);

        Assert.Throws<ConflictException>(() => item.AdjustStock(-7, "damaged"));
        Assert.Equal(10, item.QuantityOnHand);
    }

    [Fact]
    public void AdjustStock_DownToExactlyReserved_Succeeds()
    {
        var item = Item(onHand: 10, reserved: 4);

        item.AdjustStock(-6, "damaged");

        Assert.Equal(4, item.QuantityOnHand);
        Assert.Equal(0, item.AvailableQuantity);
    }

    // ---------- SetStock ----------

    [Theory]
    [InlineData(0)]
    [InlineData(25)]
    public void SetStock_ReplacesOnHand(int quantity)
    {
        var item = Item(onHand: 10);

        item.SetStock(quantity, "recount");

        Assert.Equal(quantity, item.QuantityOnHand);
    }

    [Fact]
    public void SetStock_Negative_Throws()
    {
        Assert.Throws<ValidationException>(() => Item().SetStock(-1, "recount"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetStock_BlankReason_Throws(string reason)
    {
        Assert.Throws<ValidationException>(() => Item().SetStock(5, reason));
    }

    [Fact]
    public void SetStock_BelowReserved_ThrowsConflict_AndLeavesStockUnchanged()
    {
        var item = Item(onHand: 10, reserved: 4);

        Assert.Throws<ConflictException>(() => item.SetStock(3, "recount"));
        Assert.Equal(10, item.QuantityOnHand);
    }

    // ---------- ReserveStock ----------

    [Fact]
    public void ReserveStock_ReducesAvailable_NotOnHand()
    {
        var item = Item(onHand: 10);

        item.ReserveStock(3);

        Assert.Equal(10, item.QuantityOnHand);
        Assert.Equal(3, item.QuantityReserved);
        Assert.Equal(7, item.AvailableQuantity);
    }

    [Fact]
    public void ReserveStock_AllAvailable_Succeeds()
    {
        var item = Item(onHand: 5, reserved: 2);

        item.ReserveStock(3);

        Assert.Equal(0, item.AvailableQuantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ReserveStock_NonPositive_Throws(int quantity)
    {
        Assert.Throws<ValidationException>(() => Item(onHand: 10).ReserveStock(quantity));
    }

    [Fact]
    public void ReserveStock_MoreThanAvailable_ThrowsConflict()
    {
        var item = Item(onHand: 10, reserved: 8);

        Assert.Throws<ConflictException>(() => item.ReserveStock(3));
        Assert.Equal(8, item.QuantityReserved);
    }

    // ---------- ReleaseStock ----------

    [Fact]
    public void ReleaseStock_ReturnsReservedToAvailable()
    {
        var item = Item(onHand: 10, reserved: 5);

        item.ReleaseStock(2);

        Assert.Equal(3, item.QuantityReserved);
        Assert.Equal(7, item.AvailableQuantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ReleaseStock_NonPositive_Throws(int quantity)
    {
        Assert.Throws<ValidationException>(() => Item(onHand: 10, reserved: 5).ReleaseStock(quantity));
    }

    [Fact]
    public void ReleaseStock_MoreThanReserved_ThrowsConflict()
    {
        var item = Item(onHand: 10, reserved: 2);

        Assert.Throws<ConflictException>(() => item.ReleaseStock(3));
        Assert.Equal(2, item.QuantityReserved);
    }

    // ---------- Delete ----------

    [Fact]
    public void Delete_MarksDeleted()
    {
        var item = Item(onHand: 10);

        item.Delete();

        Assert.True(item.IsDeleted);
    }

    [Fact]
    public void Delete_Twice_IsNoOp()
    {
        var item = Item();
        item.Delete();
        var updatedAt = item.UpdatedAt;

        item.Delete();

        Assert.True(item.IsDeleted);
        Assert.Equal(updatedAt, item.UpdatedAt);
    }
}
