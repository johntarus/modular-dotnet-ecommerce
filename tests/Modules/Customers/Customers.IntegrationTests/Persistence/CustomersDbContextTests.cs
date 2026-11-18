using Customers.Domain.Entities;
using Customers.Domain.Events;
using Customers.IntegrationTests.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Customers.IntegrationTests.Persistence;

public class CustomersDbContextTests(PostgresFixture db) : CustomersTestBase(db)
{
    [Fact]
    public async Task SaveChanges_PublishesCustomerRegistered_OnlyAfterTheRowIsCommitted()
    {
        var publisher = new CommitCheckingPublisher(Db);
        var customer = Customer.Create(Guid.NewGuid().ToString(), "jane@shop.test", "Jane");

        await using (var context = Db.CreateContext(publisher))
        {
            context.Customers.Add(customer);
            await context.SaveChangesAsync();
        }

        var (evt, rowExisted) = Assert.Single(publisher.Seen);
        Assert.Equal(customer.Id, Assert.IsType<CustomerRegisteredDomainEvent>(evt).CustomerId);
        Assert.True(rowExisted, "The event was published before the customer row was committed.");
    }

    [Fact]
    public async Task SaveChanges_ClearsEvents_SoTheyAreNotPublishedTwice()
    {
        var customer = Customer.Create(Guid.NewGuid().ToString(), "jane@shop.test", "Jane");

        await using (var context = NewContext())
        {
            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            customer.UpdateProfile("Janet", null, null, null);
            await context.SaveChangesAsync();
        }

        Assert.Single(Publisher.Published);
        Assert.Empty(customer.DomainEvents);
    }

    [Fact]
    public async Task SaveChanges_WhenTheInsertFails_PublishesNothing()
    {
        var subject = Guid.NewGuid().ToString();
        await SeedCustomerAsync(Guid.Parse(subject));

        await using var context = NewContext();
        context.Customers.Add(Customer.Create(subject, "dup@shop.test"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        Assert.Empty(Publisher.Published);
    }

    [Fact]
    public async Task IdentitySubject_IsUnique()
    {
        var userId = Guid.NewGuid();
        await SeedCustomerAsync(userId);

        await using var context = NewContext();
        context.Customers.Add(Customer.Create(userId.ToString()));

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        Assert.Equal(PostgresErrorCodes.UniqueViolation, Assert.IsAssignableFrom<PostgresException>(ex.InnerException).SqlState);
    }

    [Fact]
    public async Task DeletingACustomer_CascadesToAddresses()
    {
        var customer = await SeedCustomerAsync(Guid.NewGuid(), configure: c =>
        {
            AddAddress(c, "Home");
            AddAddress(c, "Office");
        });

        await using (var context = NewContext())
        {
            var tracked = await context.Customers.SingleAsync(c => c.Id == customer.Id);
            context.Customers.Remove(tracked);
            await context.SaveChangesAsync();
        }

        await using var check = NewContext();
        Assert.Equal(0, await check.CustomerAddresses.CountAsync(a => a.CustomerId == customer.Id));
    }

    [Fact]
    public async Task Addresses_RoundTrip_ThroughTheBackingField()
    {
        Guid home = default;
        var customer = await SeedCustomerAsync(Guid.NewGuid(), configure: c =>
        {
            home = AddAddress(c, "Home");
            c.SetDefaultShippingAddress(home);
        });

        var loaded = await LoadCustomerAsync(customer.Id);

        Assert.Equal(home, Assert.Single(loaded!.Addresses).Id);
        Assert.Equal(home, loaded.DefaultShippingAddress?.Id);
    }

    /// <summary>Records each event together with whether the customer row was already visible to another connection.</summary>
    private sealed class CommitCheckingPublisher(PostgresFixture db) : IPublisher
    {
        public List<(object Event, bool RowExisted)> Seen { get; } = [];

        public Task Publish(object notification, CancellationToken ct = default) =>
            Record((INotification)notification, ct);

        public Task Publish<TNotification>(TNotification notification, CancellationToken ct = default)
            where TNotification : INotification => Record(notification, ct);

        private async Task Record(INotification notification, CancellationToken ct)
        {
            var id = ((CustomerRegisteredDomainEvent)notification).CustomerId;
            await using var other = db.CreateContext();
            Seen.Add((notification, await other.Customers.AnyAsync(c => c.Id == id, ct)));
        }
    }
}
