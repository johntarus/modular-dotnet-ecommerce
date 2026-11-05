using BuildingBlocks.Infrastructure.CurrentUser;
using Catalog.Contracts;
using Catalog.Contracts.Dtos;
using Grpc.Core;
using Inventory.Contracts.Grpc;

namespace Cart.IntegrationTests.Infrastructure;

public sealed class FakeCurrentUser(Guid? userId = null) : ICurrentUser
{
    public Guid? UserId { get; set; } = userId;
}

public sealed class FakeCatalogApi : ICatalogApi
{
    private readonly Dictionary<Guid, decimal> _prices = new();

    public List<Guid> Requested { get; } = [];

    public FakeCatalogApi WithProduct(Guid productId, decimal price)
    {
        _prices[productId] = price;
        return this;
    }

    public Task<ProductDto?> GetProductByIdAsync(Guid productId, CancellationToken ct = default)
    {
        Requested.Add(productId);
        ProductDto? dto = _prices.TryGetValue(productId, out var price)
            ? new ProductDto(productId, "Product", "Description", price, Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow)
            : null;
        return Task.FromResult(dto);
    }

    public Task<bool> ProductExistsAsync(Guid productId, CancellationToken ct = default) =>
        Task.FromResult(_prices.ContainsKey(productId));
}

/// <summary>
/// Stands in for the Inventory gRPC service. Products default to "plenty in stock"
/// unless a stock level is set; every request is recorded for assertions.
/// </summary>
public sealed class FakeInventoryClient : InventoryGrpc.InventoryGrpcClient
{
    private readonly Dictionary<Guid, int> _stock = new();

    public List<CheckAvailabilityRequest> Requests { get; } = [];

    public FakeInventoryClient WithStock(Guid productId, int available)
    {
        _stock[productId] = available;
        return this;
    }

    public override AsyncUnaryCall<CheckAvailabilityResponse> CheckAvailabilityAsync(
        CheckAvailabilityRequest request, CallOptions options)
    {
        Requests.Add(request);

        var available = _stock.TryGetValue(Guid.Parse(request.ProductId), out var qty) ? qty : int.MaxValue;
        var response = new CheckAvailabilityResponse
        {
            IsAvailable = request.RequestedQuantity <= available,
            AvailableQuantity = available,
            Message = request.RequestedQuantity <= available ? "" : $"only {available} available"
        };

        return new AsyncUnaryCall<CheckAvailabilityResponse>(
            Task.FromResult(response),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });
    }
}
