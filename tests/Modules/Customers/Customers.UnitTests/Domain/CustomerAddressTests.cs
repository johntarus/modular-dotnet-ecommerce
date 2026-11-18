using Customers.Domain.Entities;
using ValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace Customers.UnitTests.Domain;

/// <summary>Address rules, exercised through the Customer aggregate root.</summary>
public class CustomerAddressTests
{
    private static Customer NewCustomer() => Customer.Create(Guid.NewGuid().ToString());

    private static Guid AddHome(Customer customer, string? label = "Home") =>
        customer.AddAddress(customer.Id, "Jane Doe", "+254700000000", "jane@shop.test",
            "1 Moi Avenue", "Nairobi", "Nairobi County", "00100", "ke", label);

    private static CustomerAddress Get(Customer customer, Guid id) => customer.Addresses.Single(a => a.Id == id);

    // ---------- Add ----------

    [Fact]
    public void AddAddress_AddsNormalizedAddress_OwnedByCustomer()
    {
        var customer = NewCustomer();

        var id = customer.AddAddress(customer.Id, "  Jane Doe ", "  ", null,
            " 1 Moi Avenue ", " Nairobi ", " Nairobi County ", "  ", " ke ", "  Home ");

        var address = Assert.Single(customer.Addresses);
        Assert.Equal(id, address.Id);
        Assert.Equal(customer.Id, address.CustomerId);
        Assert.Equal("Jane Doe", address.RecipientName);
        Assert.Null(address.PhoneNumber);
        Assert.Null(address.Email);
        Assert.Equal("1 Moi Avenue", address.Street);
        Assert.Equal("Nairobi", address.City);
        Assert.Equal("Nairobi County", address.State);
        Assert.Null(address.PostalCode);
        Assert.Equal("KE", address.CountryCode);
        Assert.Equal("Home", address.Label);
    }

    [Fact]
    public void AddAddress_IsNotDefaultByItself()
    {
        var customer = NewCustomer();
        AddHome(customer);

        Assert.Null(customer.DefaultShippingAddress);
        Assert.Null(customer.DefaultBillingAddress);
    }

    [Fact]
    public void AddAddress_Twice_KeepsBoth_WithDistinctIds()
    {
        var customer = NewCustomer();

        var first = AddHome(customer);
        var second = AddHome(customer, "Office");

        Assert.NotEqual(first, second);
        Assert.Equal(2, customer.Addresses.Count);
    }

    public static TheoryData<string, string, string, string, string, string> InvalidAddresses => new()
    {
        // recipient, street, city, state, country, expected message
        { " ", "1 Moi Ave", "Nairobi", "Nairobi", "KE", "Recipient name is required." },
        { "Jane", " ", "Nairobi", "Nairobi", "KE", "Street is required." },
        { "Jane", "1 Moi Ave", " ", "Nairobi", "KE", "City is required." },
        { "Jane", "1 Moi Ave", "Nairobi", " ", "KE", "State is required." },
        { "Jane", "1 Moi Ave", "Nairobi", "Nairobi", " ", "A valid two-character country code is required." },
        { "Jane", "1 Moi Ave", "Nairobi", "Nairobi", "KEN", "A valid two-character country code is required." },
        { "Jane", "1 Moi Ave", "Nairobi", "Nairobi", "K", "A valid two-character country code is required." },
    };

    [Theory]
    [MemberData(nameof(InvalidAddresses))]
    public void AddAddress_WithInvalidField_Throws_AndAddsNothing(
        string recipient, string street, string city, string state, string country, string message)
    {
        var customer = NewCustomer();

        var ex = Assert.Throws<ValidationException>(() =>
            customer.AddAddress(customer.Id, recipient, null, null, street, city, state, null, country));

        Assert.Equal(message, ex.Message);
        Assert.Empty(customer.Addresses);
    }

    // ---------- Update ----------

    [Fact]
    public void UpdateAddress_ReplacesFields()
    {
        var customer = NewCustomer();
        var id = AddHome(customer);

        customer.UpdateAddress(id, "John Doe", null, "john@shop.test",
            "2 Kenyatta Ave", "Mombasa", "Mombasa County", "80100", "ke", "Holiday");

        var address = Get(customer, id);
        Assert.Equal("John Doe", address.RecipientName);
        Assert.Null(address.PhoneNumber);
        Assert.Equal("john@shop.test", address.Email);
        Assert.Equal("2 Kenyatta Ave", address.Street);
        Assert.Equal("Mombasa", address.City);
        Assert.Equal("Mombasa County", address.State);
        Assert.Equal("80100", address.PostalCode);
        Assert.Equal("KE", address.CountryCode);
        Assert.Equal("Holiday", address.Label);
    }

    [Fact]
    public void UpdateAddress_KeepsDefaultFlags()
    {
        var customer = NewCustomer();
        var id = AddHome(customer);
        customer.SetDefaultShippingAddress(id);

        customer.UpdateAddress(id, "John", null, null, "2 Kenyatta Ave", "Mombasa", "Mombasa", null, "KE");

        Assert.True(Get(customer, id).IsDefaultShipping);
    }

    [Theory]
    [MemberData(nameof(InvalidAddresses))]
    public void UpdateAddress_WithInvalidField_Throws_AndLeavesAddressUnchanged(
        string recipient, string street, string city, string state, string country, string message)
    {
        var customer = NewCustomer();
        var id = AddHome(customer);

        var ex = Assert.Throws<ValidationException>(() =>
            customer.UpdateAddress(id, recipient, null, null, street, city, state, null, country));

        Assert.Equal(message, ex.Message);
        var address = Get(customer, id);
        Assert.Equal("Jane Doe", address.RecipientName);
        Assert.Equal("1 Moi Avenue", address.Street);
        Assert.Equal("Nairobi County", address.State);
    }

    [Fact]
    public void UpdateAddress_ThatBelongsToSomeoneElse_Throws()
    {
        var customer = NewCustomer();
        AddHome(customer);

        var ex = Assert.Throws<ValidationException>(() =>
            customer.UpdateAddress(Guid.NewGuid(), "John", null, null, "St", "City", "State", null, "KE"));
        Assert.Equal("The specified address does not belong to this customer.", ex.Message);
    }

    // ---------- Remove ----------

    [Fact]
    public void RemoveAddress_RemovesOnlyThatAddress()
    {
        var customer = NewCustomer();
        var home = AddHome(customer);
        var office = AddHome(customer, "Office");

        customer.RemoveAddress(home);

        Assert.Equal(office, Assert.Single(customer.Addresses).Id);
    }

    [Fact]
    public void RemoveAddress_Unknown_Throws()
    {
        var customer = NewCustomer();
        AddHome(customer);

        Assert.Throws<ValidationException>(() => customer.RemoveAddress(Guid.NewGuid()));
        Assert.Single(customer.Addresses);
    }

    [Fact]
    public void RemoveAddress_ThatWasDefault_LeavesNoDefault()
    {
        var customer = NewCustomer();
        var id = AddHome(customer);
        customer.SetDefaultShippingAddress(id);

        customer.RemoveAddress(id);

        Assert.Null(customer.DefaultShippingAddress);
    }

    // ---------- Defaults ----------

    [Fact]
    public void SetDefaultShipping_MarksOnlyThatAddress()
    {
        var customer = NewCustomer();
        var home = AddHome(customer);
        var office = AddHome(customer, "Office");

        customer.SetDefaultShippingAddress(office);

        Assert.Equal(office, customer.DefaultShippingAddress?.Id);
        Assert.False(Get(customer, home).IsDefaultShipping);
    }

    [Fact]
    public void SetDefaultShipping_Again_MovesTheDefault()
    {
        var customer = NewCustomer();
        var home = AddHome(customer);
        var office = AddHome(customer, "Office");

        customer.SetDefaultShippingAddress(home);
        customer.SetDefaultShippingAddress(office);

        Assert.Single(customer.Addresses, a => a.IsDefaultShipping);
        Assert.Equal(office, customer.DefaultShippingAddress?.Id);
    }

    [Fact]
    public void ShippingAndBillingDefaults_AreIndependent()
    {
        var customer = NewCustomer();
        var home = AddHome(customer);
        var office = AddHome(customer, "Office");

        customer.SetDefaultShippingAddress(home);
        customer.SetDefaultBillingAddress(office);

        Assert.Equal(home, customer.DefaultShippingAddress?.Id);
        Assert.Equal(office, customer.DefaultBillingAddress?.Id);
        Assert.False(Get(customer, home).IsDefaultBilling);
        Assert.False(Get(customer, office).IsDefaultShipping);
    }

    [Fact]
    public void SetDefault_UnknownAddress_Throws_AndKeepsExistingDefault()
    {
        var customer = NewCustomer();
        var home = AddHome(customer);
        customer.SetDefaultBillingAddress(home);

        Assert.Throws<ValidationException>(() => customer.SetDefaultBillingAddress(Guid.NewGuid()));
        Assert.Throws<ValidationException>(() => customer.SetDefaultShippingAddress(Guid.NewGuid()));

        Assert.Equal(home, customer.DefaultBillingAddress?.Id);
        Assert.Null(customer.DefaultShippingAddress);
    }
}
