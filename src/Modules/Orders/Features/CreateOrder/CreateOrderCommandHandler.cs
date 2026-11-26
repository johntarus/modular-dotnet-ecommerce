using BuildingBlocks.Exceptions;
using BuildingBlocks.Infrastructure.Auth.CurrentUser;
using Cart.Contracts;
using Catalog.Contracts;
using Customers.Contracts;
using MediatR;
using Orders.Domain.Entities;
using Orders.Domain.ValueObjects;
using Orders.Infrastructure.Persistence;

namespace Orders.Features.CreateOrder;

internal sealed class CreateOrderHandler(
    OrdersDbContext dbContext,
    ICustomersModuleApi customersApi,
    ICatalogApi catalogApi,
    ICartModuleApi cartApi,
    ICurrentUser currentUser
) : IRequestHandler<CreateOrderCommand, Guid>
{
    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // 1. Authenticate
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.UserId.ToString()))
            throw new UnauthorizedAccessException("User is not authenticated.");

        var identitySubject = currentUser.UserId.ToString();

        // 2. Fetch Address (Verifies ownership and returns the CustomerId)
        var address = await customersApi.GetCustomerAddressAsync(identitySubject, request.AddressId, cancellationToken);
        if (address is null)
            throw new NotFoundException("Shipping address not found.");
            
        var customerId = address.CustomerId;

        // 3. Fetch Cart (Verifies ownership and non-empty).
        // Carts are owned by the identity subject (ShoppingCart.UserId), not the Customers-module id.
        var cart = await cartApi.GetCartForCheckoutAsync(request.CartId, identitySubject, cancellationToken);
        if (cart is null)
            throw new ValidationException("Cart is empty or does not belong to you.");

        // 4. Fetch Authoritative Products
        var productIds = cart.Items.Select(x => x.ProductId).Distinct().ToArray();
        var products = await catalogApi.GetProductsByIdAsync(productIds, cancellationToken);
        var productsById = products.ToDictionary(x => x.Id);

        if (productIds.Any(id => !productsById.ContainsKey(id)))
            throw new NotFoundException("One or more products in your cart could not be found.");

        // 5. Create Order Items
        var orderItems = cart.Items.Select(item =>
        {
            var product = productsById[item.ProductId];

            if (!product.IsActive)
                throw new InvalidOperationException($"Product '{product.Name}' is not available.");

            return OrderItem.Create(
                product.Id, product.Name, product.Sku, item.Quantity, 
                Money.Create(product.Price, product.Currency));
        }).ToList();

        // 6. Currency Validation
        var orderCurrency = orderItems.First().UnitPrice.Currency;
        if (orderItems.Any(x => x.UnitPrice.Currency != orderCurrency))
            throw new InvalidOperationException("All products in an order must use the same currency.");

        // 7. Snapshot Address
        var deliveryAddress = new DeliveryAddress(
            address.RecipientName, address.PhoneNumber, address.Street, 
            address.City, address.State, address.CountryCode, address.PostalCode);

        // 8. Calculate Shipping (Server-side)
        const decimal flatShippingRate = 500m; 
        var shippingCost = Money.Create(flatShippingRate, orderCurrency);

        // 9. Create Aggregate & Save (Outbox publishes OrderCreatedEvent)
        var order = Order.Create(
            customerId, deliveryAddress, shippingCost, orderItems, DateTime.UtcNow);

        await dbContext.Orders.AddAsync(order, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return order.Id;
    }
}