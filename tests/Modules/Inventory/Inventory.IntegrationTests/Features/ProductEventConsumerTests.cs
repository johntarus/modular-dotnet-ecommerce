using Catalog.Contracts.Events;
using Inventory.Features.Inventory.EventHandlers;
using Inventory.Infrastructure.Persistence;
using Inventory.IntegrationTests.Infrastructure;
using MassTransit;
using MassTransit.Testing;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.IntegrationTests.Features;

/// <summary>
/// Runs the consumers inside MassTransit's in-memory test harness (a real consume pipeline)
/// against Postgres, so faults and redeliveries behave as they would from RabbitMQ.
/// </summary>
public class ProductEventConsumerTests(PostgresFixture db) : InventoryTestBase(db), IAsyncDisposable
{
    private ServiceProvider? _provider;

    private async Task<ITestHarness> StartHarnessAsync()
    {
        _provider = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IPublisher, NoOpPublisher>()
            .AddDbContext<InventoryDbContext>(o => o.UseNpgsql(Db.ConnectionString))
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<ProductCreatedIntegrationEventConsumer>();
                x.AddConsumer<ProductDeletedIntegrationEventConsumer>();
            })
            .BuildServiceProvider(validateScopes: true);

        var harness = _provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        return harness;
    }

    public async ValueTask DisposeAsync()
    {
        if (_provider is not null) await _provider.DisposeAsync();
    }

    private static ProductCreatedIntegrationEvent Created(Guid productId) =>
        new(productId, "Product", 10m, Guid.NewGuid(), DateTime.UtcNow);

    private static ProductDeletedIntegrationEvent Deleted(Guid productId) => new(productId, DateTime.UtcNow);

    /// <summary>
    /// Waits until <paramref name="expected"/> messages of type T have been consumed, then reports
    /// whether any of them faulted. (harness.InactivityTask only completes once, so it can't be
    /// used to wait for a second message.)
    /// </summary>
    private static async Task<bool> AnyConsumerFaultedAsync<T>(ITestHarness harness, int expected = 1) where T : class
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (harness.Consumed.Select<T>().Count() < expected)
        {
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException($"Only {harness.Consumed.Select<T>().Count()} of {expected} {typeof(T).Name} consumed.");
            await Task.Delay(50);
        }
        return harness.Consumed.Select<T>().Any(x => x.Exception is not null);
    }

    // ---------- ProductCreated ----------

    [Fact]
    public async Task ProductCreated_CreatesEmptyInventoryRecord()
    {
        var harness = await StartHarnessAsync();
        var productId = Guid.NewGuid();

        await harness.Bus.Publish(Created(productId));

        Assert.False(await AnyConsumerFaultedAsync<ProductCreatedIntegrationEvent>(harness));
        var item = await LoadAsync(productId);
        Assert.NotNull(item);
        Assert.Equal(0, item.QuantityOnHand);
        Assert.False(item.IsDeleted);
    }

    [Fact]
    public async Task ProductCreated_Redelivered_IsSkipped_WithoutFaulting()
    {
        var harness = await StartHarnessAsync();
        var productId = Guid.NewGuid();

        await harness.Bus.Publish(Created(productId));
        await AnyConsumerFaultedAsync<ProductCreatedIntegrationEvent>(harness, expected: 1);
        await harness.Bus.Publish(Created(productId));

        Assert.False(await AnyConsumerFaultedAsync<ProductCreatedIntegrationEvent>(harness, expected: 2));
        Assert.Equal(1, await CountAsync());
    }

    [Fact]
    public async Task ProductCreated_Redelivered_DoesNotResetExistingStock()
    {
        var productId = await SeedItemAsync(onHand: 25);
        var harness = await StartHarnessAsync();

        await harness.Bus.Publish(Created(productId));

        Assert.False(await AnyConsumerFaultedAsync<ProductCreatedIntegrationEvent>(harness));
        Assert.Equal(25, (await LoadAsync(productId))!.QuantityOnHand);
    }

    [Fact]
    public async Task ProductCreated_ForSoftDeletedRecord_IsSkipped_AndStaysDeleted()
    {
        var productId = await SeedItemAsync(deleted: true);
        var harness = await StartHarnessAsync();

        await harness.Bus.Publish(Created(productId));

        Assert.False(await AnyConsumerFaultedAsync<ProductCreatedIntegrationEvent>(harness));
        Assert.True((await LoadAsync(productId))!.IsDeleted);
    }

    // ---------- ProductDeleted ----------

    [Fact]
    public async Task ProductDeleted_SoftDeletesRecord_KeepingItsData()
    {
        var productId = await SeedItemAsync(onHand: 10);
        var harness = await StartHarnessAsync();

        await harness.Bus.Publish(Deleted(productId));

        Assert.False(await AnyConsumerFaultedAsync<ProductDeletedIntegrationEvent>(harness));
        var item = (await LoadAsync(productId))!;
        Assert.True(item.IsDeleted);
        Assert.Equal(10, item.QuantityOnHand);
    }

    [Fact]
    public async Task ProductDeleted_Redelivered_IsNoOp()
    {
        var productId = await SeedItemAsync(onHand: 10);
        var harness = await StartHarnessAsync();

        await harness.Bus.Publish(Deleted(productId));
        await AnyConsumerFaultedAsync<ProductDeletedIntegrationEvent>(harness, expected: 1);
        await harness.Bus.Publish(Deleted(productId));

        Assert.False(await AnyConsumerFaultedAsync<ProductDeletedIntegrationEvent>(harness, expected: 2));
        Assert.True((await LoadAsync(productId))!.IsDeleted);
    }

    [Fact]
    public async Task ProductDeleted_ForUnknownProduct_IsNoOp()
    {
        var harness = await StartHarnessAsync();

        await harness.Bus.Publish(Deleted(Guid.NewGuid()));

        Assert.False(await AnyConsumerFaultedAsync<ProductDeletedIntegrationEvent>(harness));
        Assert.Equal(0, await CountAsync());
    }
}
