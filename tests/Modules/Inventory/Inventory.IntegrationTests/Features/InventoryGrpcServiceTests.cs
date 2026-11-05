using Grpc.Core;
using Inventory.Contracts.Grpc;
using Inventory.Features.Inventory.Grpc;
using Inventory.IntegrationTests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace Inventory.IntegrationTests.Features;

/// <summary>The gRPC endpoint Cart calls before adding to or changing a cart.</summary>
public class InventoryGrpcServiceTests(PostgresFixture db) : InventoryTestBase(db)
{
    private async Task<CheckAvailabilityResponse> CheckAsync(string productId, int quantity)
    {
        await using var context = NewContext();
        var service = new InventoryGrpcService(context, NullLogger<InventoryGrpcService>.Instance);
        return await service.CheckAvailability(
            new CheckAvailabilityRequest { ProductId = productId, RequestedQuantity = quantity },
            new TestServerCallContext());
    }

    [Theory]
    [InlineData(5, true)]
    [InlineData(10, true)]  // exactly what's available
    [InlineData(11, false)]
    public async Task ComparesRequestAgainstAvailable(int requested, bool expected)
    {
        var productId = await SeedItemAsync(onHand: 10);

        var response = await CheckAsync(productId.ToString(), requested);

        Assert.Equal(expected, response.IsAvailable);
        Assert.Equal(10, response.AvailableQuantity);
    }

    [Fact]
    public async Task ReservedStock_IsNotAvailable()
    {
        var productId = await SeedItemAsync(onHand: 10, reserved: 7);

        var response = await CheckAsync(productId.ToString(), 4);

        Assert.False(response.IsAvailable);
        Assert.Equal(3, response.AvailableQuantity);
    }

    [Fact]
    public async Task UnknownProduct_IsUnavailable()
    {
        var response = await CheckAsync(Guid.NewGuid().ToString(), 1);

        Assert.False(response.IsAvailable);
        Assert.Equal(0, response.AvailableQuantity);
    }

    [Fact]
    public async Task DeletedProduct_IsUnavailable_EvenWithStock()
    {
        var productId = await SeedItemAsync(onHand: 10, deleted: true);

        var response = await CheckAsync(productId.ToString(), 1);

        Assert.False(response.IsAvailable);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("")]
    public async Task InvalidProductId_ThrowsInvalidArgument(string productId)
    {
        var ex = await Assert.ThrowsAsync<RpcException>(() => CheckAsync(productId, 1));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
    }
}
