using System.ComponentModel.DataAnnotations;

namespace Cart.Domain.Entities;

public class CartItem
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid CartId { get; private set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; private set; }
    
    public CartItem(Guid productId, int quantity, decimal unitPrice)
    {
        if (productId == Guid.Empty)
            throw new ValidationException("ProductId is required.");

        if (quantity <= 0)
            throw new ValidationException(
                "Quantity must be greater than zero.");

        if (unitPrice < 0)
            throw new ValidationException("Price cannot be negative.");
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
    
    public void Increase(int quantity, decimal unitPrice)
    {
        if (quantity <= 0)
            throw new ValidationException(
                "Quantity must be greater than zero.");

        if (unitPrice < 0)
            throw new ValidationException("Price cannot be negative.");

        Quantity += quantity;
        UnitPrice = unitPrice;
    }

    public void SetQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new ValidationException("Quantity must be greater than zero.");
        // if (unitPrice < 0)
        //     throw new ValidationException("Price cannot be negative.");
        Quantity = quantity;
        // UnitPrice = unitPrice;
    }
}