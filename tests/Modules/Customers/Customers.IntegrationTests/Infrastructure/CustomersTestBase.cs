using Customers.Domain.Entities;
using Customers.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Customers.IntegrationTests.Infrastructure;

[Collection(PostgresCollection.Name)]
public abstract class CustomersTestBase(PostgresFixture db) : IAsyncLifetime
{
    protected PostgresFixture Db { get; } = db;
    protected FakeCurrentUser CurrentUser { get; } = new();
    protected RecordingPublisher Publisher { get; } = new();

    public Task InitializeAsync() => Db.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    internal CustomersDbContext NewContext() => Db.CreateContext(Publisher);

    /// <summary>Seeds a customer whose IdentitySubject is a fresh user id, and signs that user in.</summary>
    protected async Task<Customer> SignInAsNewCustomerAsync(string? email = "jane@shop.test", string? firstName = "Jane")
    {
        var userId = Guid.NewGuid();
        var customer = await SeedCustomerAsync(userId, email, firstName);
        CurrentUser.SignIn(userId);
        return customer;
    }

    protected async Task<Customer> SeedCustomerAsync(Guid userId, string? email = "jane@shop.test", string? firstName = "Jane",
        Action<Customer>? configure = null)
    {
        var customer = Customer.Create(userId.ToString(), email, firstName, "Doe");
        configure?.Invoke(customer);

        await using var context = Db.CreateContext();
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        return customer;
    }

    protected static Guid AddAddress(Customer customer, string label = "Home") =>
        customer.AddAddress(customer.Id, "Jane Doe", "+254700000000", "jane@shop.test",
            "1 Moi Avenue", "Nairobi", "Nairobi County", "00100", "KE", label);

    /// <summary>Reads a customer straight from the database, bypassing any tracked state.</summary>
    protected async Task<Customer?> LoadCustomerAsync(Guid customerId)
    {
        await using var context = NewContext();
        return await context.Customers.AsNoTracking().Include(c => c.Addresses)
            .SingleOrDefaultAsync(c => c.Id == customerId);
    }
}
