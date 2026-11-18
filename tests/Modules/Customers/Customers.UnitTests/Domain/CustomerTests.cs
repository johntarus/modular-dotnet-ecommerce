using Customers.Domain.Entities;
using Customers.Domain.Events;
using ValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace Customers.UnitTests.Domain;

public class CustomerTests
{
    private const string Subject = "6f1c2a9e-0d3b-4c7e-9a51-2b8f4e6d7c10";

    // ---------- Create ----------

    [Fact]
    public void Create_SetsIdentityAndProfile_AndIsActive()
    {
        var customer = Customer.Create(Subject, "jane@shop.test", "Jane", "Doe");

        Assert.NotEqual(Guid.Empty, customer.Id);
        Assert.Equal(Subject, customer.IdentitySubject);
        Assert.Equal("jane@shop.test", customer.Email);
        Assert.Equal("Jane", customer.FirstName);
        Assert.Equal("Doe", customer.LastName);
        Assert.Null(customer.PhoneNumber);
        Assert.True(customer.IsActive);
        Assert.Empty(customer.Addresses);
    }

    [Fact]
    public void Create_TrimsValues_AndTurnsBlanksIntoNull()
    {
        var customer = Customer.Create(Subject, "  jane@shop.test ", "   ", " Doe ");

        Assert.Equal("jane@shop.test", customer.Email);
        Assert.Null(customer.FirstName);
        Assert.Equal("Doe", customer.LastName);
    }

    [Fact]
    public void Create_RaisesCustomerRegisteredEvent_WithCustomerDetails()
    {
        var customer = Customer.Create(Subject, "jane@shop.test", "Jane", "Doe");

        var evt = Assert.IsType<CustomerRegisteredDomainEvent>(Assert.Single(customer.DomainEvents));
        Assert.Equal(customer.Id, evt.CustomerId);
        Assert.Equal("jane@shop.test", evt.Email);
        Assert.Equal("Jane", evt.FirstName);
    }

    [Fact]
    public void Create_WithoutEmailOrName_StillRegisters_WithNullsOnTheEvent()
    {
        var customer = Customer.Create(Subject);

        var evt = Assert.IsType<CustomerRegisteredDomainEvent>(Assert.Single(customer.DomainEvents));
        Assert.Null(evt.Email);
        Assert.Null(evt.FirstName);
    }

    [Fact]
    public void Create_EachCustomerGetsItsOwnId()
    {
        Assert.NotEqual(Customer.Create(Subject).Id, Customer.Create(Subject).Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithoutIdentitySubject_Throws(string? subject)
    {
        var ex = Assert.Throws<ValidationException>(() => Customer.Create(subject!));
        Assert.Equal("Identity subject is required.", ex.Message);
    }

    // ---------- Profile ----------

    [Fact]
    public void UpdateProfile_ReplacesAllFields_Normalized()
    {
        var customer = Customer.Create(Subject, "old@shop.test", "Old", "Name");

        customer.UpdateProfile(" New ", "  ", "new@shop.test", " +254700000000 ");

        Assert.Equal("New", customer.FirstName);
        Assert.Null(customer.LastName);
        Assert.Equal("new@shop.test", customer.Email);
        Assert.Equal("+254700000000", customer.PhoneNumber);
    }

    [Fact]
    public void UpdateProfile_TouchesUpdatedAt()
    {
        var customer = Customer.Create(Subject);
        var before = customer.UpdatedAt;
        Thread.Sleep(2);

        customer.UpdateProfile("A", "B", null, null);

        Assert.True(customer.UpdatedAt > before);
    }

    // ---------- Activation ----------

    [Fact]
    public void Deactivate_ThenActivate_TogglesIsActive()
    {
        var customer = Customer.Create(Subject);

        customer.Deactivate();
        Assert.False(customer.IsActive);

        customer.Activate();
        Assert.True(customer.IsActive);
    }

    [Fact]
    public void Activate_WhenAlreadyActive_IsANoOp()
    {
        var customer = Customer.Create(Subject);
        var before = customer.UpdatedAt;
        Thread.Sleep(2);

        customer.Activate();

        Assert.True(customer.IsActive);
        Assert.Equal(before, customer.UpdatedAt);
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_IsANoOp()
    {
        var customer = Customer.Create(Subject);
        customer.Deactivate();
        var before = customer.UpdatedAt;
        Thread.Sleep(2);

        customer.Deactivate();

        Assert.False(customer.IsActive);
        Assert.Equal(before, customer.UpdatedAt);
    }
}
