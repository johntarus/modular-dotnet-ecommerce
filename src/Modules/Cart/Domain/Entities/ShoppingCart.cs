using System.ComponentModel.DataAnnotations;
using BuildingBlocks.Domain;

namespace Cart.Domain.Entities;

public class ShoppingCart : BaseEntity
{
    private readonly List<CartItem> _items = [];
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public uint Version { get; private set; }

    public ShoppingCart()
    {
    }

    public static ShoppingCart Create(Guid userId)
    {
        if (userId == Guid.Empty) throw new ValidationException("UserId is required.");
        return new ShoppingCart
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void AddItem(Guid productId, int quantity, decimal unitPrice)
    {
        if (quantity <= 0) throw new ValidationException("Quantity must be greater than zero.");
        if (unitPrice < 0) throw new ValidationException("Price cannot be negative.");
        var existingItem = _items.FirstOrDefault(x => x.ProductId == productId);
        if (existingItem is null)
            _items.Add(new CartItem(productId, quantity, unitPrice));
        else
            existingItem.Increase(quantity, unitPrice);

        UpdatedAt = DateTime.UtcNow;
    }
}