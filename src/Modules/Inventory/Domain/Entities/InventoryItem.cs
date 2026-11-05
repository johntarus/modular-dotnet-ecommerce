using BuildingBlocks.Domain;
using BuildingBlocks.Exceptions;

namespace Inventory.Domain.Entities;

public class InventoryItem : BaseEntity
{
    public Guid ProductId { get; private set; }
    public int QuantityOnHand { get; private set; }
    public int QuantityReserved { get; private set; }
    public int AvailableQuantity => QuantityOnHand - QuantityReserved;

    public bool IsDeleted { get; private set; }

    /// <summary>Concurrency token (Postgres xmin): stops concurrent stock changes overwriting each other.</summary>
    public uint Version { get; private set; }

    private InventoryItem()
    {
    }

    public static InventoryItem Create(Guid productId)
    {
        return new InventoryItem
        {
            ProductId = productId,
            QuantityOnHand = 0,
            QuantityReserved = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public void AdjustStock(int delta, string reason)
    {
        if (delta == 0) throw new ValidationException("Delta cannot be zero.");
        if (string.IsNullOrWhiteSpace(reason)) throw new ValidationException("A reason is required.");
        if (QuantityOnHand + delta < QuantityReserved)
            throw new ConflictException("Stock cannot drop below the reserved quantity.");

        QuantityOnHand += delta;
        UpdatedAt = DateTime.UtcNow;
        // AddDomainEvent(new StockAdjustedDomainEvent(ProductId, delta, QuantityOnHand, reason));
    }

    public void SetStock(int quantity, string reason)
    {
        if (quantity < 0) throw new ValidationException("Quantity cannot be negative.");
        if (string.IsNullOrWhiteSpace(reason)) throw new ValidationException("A reason is required.");
        if (quantity < QuantityReserved)
            throw new ConflictException("Stock cannot be set below the reserved quantity.");

        QuantityOnHand = quantity;
        UpdatedAt = DateTime.UtcNow;
        // AddDomainEvent(new StockAdjustedDomainEvent(ProductId, delta, QuantityOnHand, reason));
    }

    public void ReserveStock(int quantity)
    {
        if (quantity <= 0)
            throw new ValidationException(
                "Reservation quantity must be positive.");

        if (quantity > AvailableQuantity)
            throw new ConflictException("Insufficient available stock.");

        QuantityReserved += quantity;
    }

    public void ReleaseStock(int quantity)
    {
        if (quantity <= 0)
            throw new ValidationException(
                "Release quantity must be positive.");

        if (quantity > QuantityReserved)
            throw new ConflictException(
                "Cannot release more stock than is reserved.");

        QuantityReserved -= quantity;
    }

    public void Delete()
    {
        if (IsDeleted) return;
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }
}