using BuildingBlocks.Exceptions;

namespace Cart.Domain.Entities;

public class CartItem
{
    public const int MaxQuantity = 99;

    public Guid Id { get; private set; }
    public Guid CartId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    private CartItem() { }   // for EF

    public CartItem(Guid productId, int quantity, decimal unitPrice)
    {
        if (productId == Guid.Empty)
            throw new ValidationException("ProductId is required.");
        if (quantity <= 0)
            throw new ValidationException("Quantity must be greater than zero.");
        if (quantity > MaxQuantity)
            throw new ValidationException($"Maximum quantity per item is {MaxQuantity}.");
        if (unitPrice < 0)
            throw new ValidationException("Price cannot be negative.");

        Id = Guid.NewGuid();
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public void Increase(int quantity, decimal unitPrice)
    {
        if (quantity <= 0)
            throw new ValidationException("Quantity must be greater than zero.");
        if (unitPrice < 0)
            throw new ValidationException("Price cannot be negative.");
        if (Quantity + quantity > MaxQuantity)
            throw new ValidationException($"Maximum quantity per item is {MaxQuantity}.");

        Quantity += quantity;
        UnitPrice = unitPrice;
    }

    public void SetQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new ValidationException("Quantity must be greater than zero.");
        if (quantity > MaxQuantity)
            throw new ValidationException($"Maximum quantity per item is {MaxQuantity}.");

        Quantity = quantity;
    }
}