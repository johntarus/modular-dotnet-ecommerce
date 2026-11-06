using BuildingBlocks.Domain;
using BuildingBlocks.Exceptions;

namespace Cart.Domain.Entities;

public class ShoppingCart : BaseEntity
{
    private readonly List<CartItem> _items = [];
    public Guid? UserId { get; private set; }
    public string? GuestTokenHash  { get; private set; }
    public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();
    public DateTime ExpiresAt  { get; private set; }
    public uint Version { get; private set; }

    /// <summary>A guest cart expires this long after its last change.</summary>
    public static readonly TimeSpan GuestCartLifetime = TimeSpan.FromHours(24);

    public ShoppingCart()
    {
    }

    public static ShoppingCart CreateForUser(Guid userId)
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

    public static ShoppingCart CreateForGuest(string guestToken) => new()
    {
        Id = Guid.NewGuid(),
        UserId = null,
        GuestTokenHash = guestToken,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        ExpiresAt = DateTime.UtcNow.Add(GuestCartLifetime),
    };

    public void AddItem(Guid productId, int quantity, decimal unitPrice)
    {
        if (quantity <= 0) throw new ValidationException("Quantity must be greater than zero.");
        if (unitPrice < 0) throw new ValidationException("Price cannot be negative.");
        var existingItem = _items.FirstOrDefault(x => x.ProductId == productId);
        if (existingItem is null)
            _items.Add(new CartItem(productId, quantity, unitPrice));
        else
            existingItem.Increase(quantity, unitPrice);

        MarkUpdated();
    }
    
    public void SetItemQuantity(Guid productId, int quantity)
    {
        if (productId == Guid.Empty)
            throw new ValidationException("ProductId is required.");

        if (quantity <= 0)
            throw new ValidationException("Quantity must be greater than zero.");

        var item = _items.FirstOrDefault(
            x => x.ProductId == productId);

        if (item is null)
            throw new NotFoundException("The product does not exist in the cart.");

        item.SetQuantity(quantity);

        MarkUpdated();
    }

    public void RemoveItem(Guid productId)
    {
        if (productId == Guid.Empty)
            throw new ValidationException("ProductId is required.");

        var item = _items.FirstOrDefault(
            x => x.ProductId == productId);

        if (item is null)
            throw new NotFoundException("The product does not exist in the cart.");

        _items.Remove(item);

        MarkUpdated();
    }

    /// <summary>Records a change; for a guest cart this also pushes its expiry back (sliding expiry).</summary>
    private void MarkUpdated()
    {
        UpdatedAt = DateTime.UtcNow;
        if (GuestTokenHash is not null)
            ExpiresAt = UpdatedAt.Add(GuestCartLifetime);
    }

    public void Clear()
    {
        if (_items.Count == 0)
            return;

        _items.Clear();
        MarkUpdated();
    }
}