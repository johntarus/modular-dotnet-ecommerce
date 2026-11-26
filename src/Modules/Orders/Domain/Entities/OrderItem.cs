using System.ComponentModel.DataAnnotations;
using Orders.Domain.ValueObjects;

namespace Orders.Domain.Entities;

public sealed class OrderItem
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = null;
    public string Sku { get; private set; } = null;
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; } = null!;
    public Money LineTotal => UnitPrice.Multiply(Quantity);

    private OrderItem()
    {
    }

    private OrderItem(
        Guid productId,
        string productName,
        string sku,
        int quantity,
        Money unitPrice)
    {
        ProductId = productId;
        ProductName = productName;
        Sku = sku;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public static OrderItem Create(
        Guid productId,
        string productName,
        string sku,
        int quantity,
        Money unitPrice
    )
    {
        if (productId == Guid.Empty)
            throw new ValidationException("Invalid Product ID");

        if (string.IsNullOrWhiteSpace(productName))
            throw new ValidationException("Product name is required");

        if (string.IsNullOrWhiteSpace(sku))
            throw new ValidationException("Product SKU is required");

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        ArgumentNullException.ThrowIfNull(unitPrice);

        return new OrderItem(
            productId,
            productName,
            sku, quantity, unitPrice);
    }
}