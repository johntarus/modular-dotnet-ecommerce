using BuildingBlocks.Domain;

namespace Inventory.Domain.Entities;

public class InventoryItem : BaseEntity
{
    public Guid ProductId { get; private set; }
    public int QuantityOnHand { get; private set; }
    public int QuantityReserved { get; private set; }
    public int AvailableQuantity => QuantityOnHand - QuantityReserved;
    public DateTime CreatedAt { get; private set; }
    public DateTime Updated { get; private set; }
    public bool IsDeleted { get; private set; }

    private InventoryItem() { }

    public static InventoryItem Create(Guid productId)
    {
        return new InventoryItem
        {
            ProductId = productId,
            QuantityOnHand = 0,
            QuantityReserved = 0,
            CreatedAt = DateTime.UtcNow,
            Updated = DateTime.UtcNow,
            IsDeleted = false
        };
    }
    
    public void AdjustStock(int quantity)
    {
        if (quantity <= 0 && Math.Abs(quantity) > QuantityOnHand)
            throw new InvalidOperationException("Cannot reduce stock below zero.");
        QuantityOnHand += quantity;
        Updated = DateTime.UtcNow;
        // Add Domain event in future i.e #StockLevelChangedDomainEvent
    }
    
    public void ReserveStock(int quantity)
    {
        if (quantity <= 0)
            throw new InvalidOperationException("Cannot reserve negative quantity.");
        QuantityReserved += quantity;
    }
    
    public void ReleaseStock(int quantity)
    {
        if (quantity <= 0)
            throw new InvalidOperationException("Cannot release negative quantity.");
    }
    
    public void Delete()
    {
        if (IsDeleted) return;
        IsDeleted = true;
        Updated = DateTime.UtcNow;
    }
}