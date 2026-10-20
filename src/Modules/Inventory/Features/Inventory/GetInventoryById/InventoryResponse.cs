namespace Inventory.Features.Inventory.GetInventoryById
{
    public record InventoryResponse(
        Guid ProductId,
        int QuantityOnHand,
        int QuantityReserved,
        int AvailableQuantity,
        DateTime UpdatedAt);
}