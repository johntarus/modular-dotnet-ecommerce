using BuildingBlocks.Domain;
using BuildingBlocks.Exceptions;
using Orders.Domain.Enums;
using Orders.Domain.Events;
using Orders.Domain.ValueObjects;

namespace Orders.Domain.Entities;

public sealed class Order : BaseEntity
{
    private readonly List<OrderItem> _items = [];
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public DeliveryAddress DeliveryAddress { get; private set; } = null!;
    public Money ShippingCost { get; private set; } = null!;
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public Money TotalAmount
    {
        get
        {
            var total = Money.Create(0, ShippingCost.Currency);
            foreach (var item in _items)
            {
                total = total.Add(item.LineTotal);
            }

            return total;
        }
    }

    public DateTime CreatedAt { get; private set; }

    public DateTime? ConfirmedAt { get; private set; }

    public DateTime? CancelledAt { get; private set; }

    public string? CancellationReason { get; private set; }

    private Order()
    {
    }

    private Order(
        Guid customerId,
        DeliveryAddress deliveryAddress,
        Money shippingCost,
        IEnumerable<OrderItem> items,
        DateTime createdAt)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        DeliveryAddress = deliveryAddress;
        ShippingCost = shippingCost;
        CreatedAt = createdAt;
        Status = OrderStatus.Pending;
        _items.AddRange(items);
    }

    public static Order Create(
        Guid customerId,
        DeliveryAddress deliveryAddress,
        Money shippingCost,
        IEnumerable<OrderItem> items,
        DateTime createdAt)
    {
        if(customerId == Guid.Empty)
            throw new ValidationException("Invalid Customer Id");
        
        ArgumentNullException.ThrowIfNull(deliveryAddress);
        ArgumentNullException.ThrowIfNull(shippingCost);
        ArgumentNullException.ThrowIfNull(items);

        var orderItems = items.ToList();

        if (orderItems.Count == 0)
            throw new ValidationException("An order must contain at least one item.");

        if (orderItems.Any(item => item is null))
            throw new ValidationException("Order items cannot contain null values.");

        if (orderItems.Any(item => item.LineTotal.Currency != shippingCost.Currency))
            throw new ValidationException("All order items and shipping costs must use the same currency.");

        var order = new Order(
            customerId, deliveryAddress, shippingCost, orderItems, createdAt);
        
        order.AddDomainEvent(new OrderCreatedDomainEvent(order.Id, order.CustomerId));
        
        return order;
    }
    
    public void MarkAwaitingPayment()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException(
                "Only pending orders can move to awaiting payment.");

        Status = OrderStatus.AwaitingPayment;

        AddDomainEvent(
            new OrderAwaitingPaymentDomainEvent(
                Id, CustomerId));
    }
    
    public void Cancel(string reason)
    {
        if (Status is not (OrderStatus.Pending or OrderStatus.AwaitingPayment))
            throw new ValidationException("Only pending or awaiting payment orders can be cancelled.");
        
        if(string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Cancellation reason is required.", nameof(reason));
        
        CancellationReason = reason.Trim();
        CancelledAt =  DateTime.UtcNow;
        Status = OrderStatus.Cancelled;
        
        AddDomainEvent(new OrderCancelledDomainEvent(Id, CustomerId, reason));
    }
}