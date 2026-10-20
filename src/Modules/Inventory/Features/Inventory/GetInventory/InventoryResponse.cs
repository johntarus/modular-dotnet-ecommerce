namespace Inventory.Features.Inventory.GetInventory
{
    public record InventoryResponse(
        Guid ProductId,
        int QuantityOnHand,
        int QuantityReserved,
        int AvailableQuantity,
        DateTime UpdatedAt);
}