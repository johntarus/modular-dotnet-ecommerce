using Customers.Features.Addresses.AddAddress;
using Customers.Features.Addresses.DeleteAddress;
using Customers.Features.Addresses.GetAddressById;
using Customers.Features.Addresses.GetAddresses;
using Customers.Features.Addresses.SetDefaultAddress;
using Customers.Features.Addresses.UpdateAddress;
using Customers.IntegrationTests.Infrastructure;
using BuildingBlocks.Exceptions;
using ValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace Customers.IntegrationTests.Features;

public class AddressTests(PostgresFixture db) : CustomersTestBase(db)
{
    private static AddAddressCommand ValidAdd(string label = "Home") =>
        new("Jane Doe", "+254700000000", "jane@shop.test", "1 Moi Avenue", "Nairobi", "Nairobi County", "00100", "ke", label);

    // ---------- Add ----------

    [Fact]
    public async Task AddAddress_PersistsAddressForSignedInCustomer()
    {
        var me = await SignInAsNewCustomerAsync();

        Guid id;
        await using (var context = NewContext())
            id = await new AddAddressCommandHandler(context, CurrentUser).Handle(ValidAdd(), default);

        var saved = await LoadCustomerAsync(me.Id);
        var address = Assert.Single(saved!.Addresses);
        Assert.Equal(id, address.Id);
        Assert.Equal(me.Id, address.CustomerId);
        Assert.Equal("KE", address.CountryCode);
        Assert.Equal("Home", address.Label);
    }

    [Fact]
    public async Task AddAddress_ToCustomerWithExistingAddresses_KeepsThem()
    {
        var me = await SignInAsNewCustomerAsync();

        await using (var context = NewContext())
        {
            var handler = new AddAddressCommandHandler(context, CurrentUser);
            await handler.Handle(ValidAdd("Home"), default);
        }
        await using (var context = NewContext())
        {
            var handler = new AddAddressCommandHandler(context, CurrentUser);
            await handler.Handle(ValidAdd("Office"), default);
        }

        var saved = await LoadCustomerAsync(me.Id);
        Assert.Equal(["Home", "Office"], saved!.Addresses.Select(a => a.Label).Order());
    }

    [Fact]
    public async Task AddAddress_Invalid_ThrowsAndSavesNothing()
    {
        var me = await SignInAsNewCustomerAsync();

        await using (var context = NewContext())
            await Assert.ThrowsAsync<ValidationException>(() => new AddAddressCommandHandler(context, CurrentUser)
                .Handle(ValidAdd() with { CountryCode = "KEN" }, default));

        Assert.Empty((await LoadCustomerAsync(me.Id))!.Addresses);
    }

    // ---------- Read ----------

    [Fact]
    public async Task GetAddresses_ReturnsOnlyMyAddresses()
    {
        await SeedCustomerAsync(Guid.NewGuid(), configure: c => AddAddress(c, "Not mine"));
        Guid home = default, office = default;
        var userId = Guid.NewGuid();
        await SeedCustomerAsync(userId, configure: c =>
        {
            home = AddAddress(c, "Home");
            office = AddAddress(c, "Office");
            c.SetDefaultShippingAddress(office);
        });
        CurrentUser.SignIn(userId);

        await using var context = NewContext();
        var addresses = await new GetAddressesQueryHandler(context, CurrentUser).Handle(new GetAddressesQuery(), default);

        Assert.Equal(new[] { home, office }.Order(), addresses.Select(a => a.Id).Order());
        var officeResponse = addresses.Single(a => a.Id == office);
        Assert.True(officeResponse.IsDefaultShipping);
        Assert.False(officeResponse.IsDefaultBilling);
        Assert.Equal("Jane Doe", officeResponse.RecipientName);
        Assert.Equal("KE", officeResponse.CountryCode);
    }

    [Fact]
    public async Task GetAddresses_NoAddresses_ReturnsEmpty()
    {
        await SignInAsNewCustomerAsync();

        await using var context = NewContext();
        Assert.Empty(await new GetAddressesQueryHandler(context, CurrentUser).Handle(new GetAddressesQuery(), default));
    }

    [Fact]
    public async Task GetAddress_ReturnsMyAddress()
    {
        Guid id = default;
        var userId = Guid.NewGuid();
        await SeedCustomerAsync(userId, configure: c => id = AddAddress(c));
        CurrentUser.SignIn(userId);

        await using var context = NewContext();
        var address = await new GetAddressQueryHandler(context, CurrentUser).Handle(new GetAddressQuery(id), default);

        Assert.Equal(id, address.Id);
        Assert.Equal("1 Moi Avenue", address.Street);
        Assert.Equal("Nairobi", address.City);
        Assert.Equal("Nairobi County", address.State);
    }

    [Fact]
    public async Task GetAddress_OfAnotherCustomer_IsNotFound()
    {
        Guid theirs = default;
        await SeedCustomerAsync(Guid.NewGuid(), configure: c => theirs = AddAddress(c));
        await SignInAsNewCustomerAsync();

        await using var context = NewContext();
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new GetAddressQueryHandler(context, CurrentUser).Handle(new GetAddressQuery(theirs), default));
    }

    // ---------- Update ----------

    [Fact]
    public async Task UpdateAddress_PersistsChanges()
    {
        Guid id = default;
        var userId = Guid.NewGuid();
        var me = await SeedCustomerAsync(userId, configure: c => id = AddAddress(c));
        CurrentUser.SignIn(userId);

        await using (var context = NewContext())
            await new UpdateAddressCommandHandler(context, CurrentUser).Handle(
                new UpdateAddressCommand(id, "John Doe", "+254711111111", null, "2 Kenyatta Ave", "Mombasa", "Mombasa County", "80100", "KE", "Holiday"),
                default);

        var address = Assert.Single((await LoadCustomerAsync(me.Id))!.Addresses);
        Assert.Equal("John Doe", address.RecipientName);
        Assert.Equal("2 Kenyatta Ave", address.Street);
        Assert.Equal("Mombasa", address.City);
        Assert.Equal("Holiday", address.Label);
    }

    [Fact]
    public async Task UpdateAddress_OfAnotherCustomer_IsRejected_AndLeftUnchanged()
    {
        Guid theirs = default;
        var other = await SeedCustomerAsync(Guid.NewGuid(), configure: c => theirs = AddAddress(c));
        await SignInAsNewCustomerAsync();

        await using (var context = NewContext())
            await Assert.ThrowsAsync<ValidationException>(() => new UpdateAddressCommandHandler(context, CurrentUser).Handle(
                new UpdateAddressCommand(theirs, "Hacker", null, null, "X", "X", "X", null, "KE", null), default));

        Assert.Equal("Jane Doe", Assert.Single((await LoadCustomerAsync(other.Id))!.Addresses).RecipientName);
    }

    // ---------- Delete ----------

    [Fact]
    public async Task DeleteAddress_RemovesItFromTheDatabase()
    {
        Guid home = default, office = default;
        var userId = Guid.NewGuid();
        var me = await SeedCustomerAsync(userId, configure: c =>
        {
            home = AddAddress(c, "Home");
            office = AddAddress(c, "Office");
        });
        CurrentUser.SignIn(userId);

        await using (var context = NewContext())
            await new DeleteAddressCommandHandler(context, CurrentUser).Handle(new DeleteAddressCommand(home), default);

        Assert.Equal(office, Assert.Single((await LoadCustomerAsync(me.Id))!.Addresses).Id);
    }

    [Fact]
    public async Task DeleteAddress_OfAnotherCustomer_IsRejected_AndKept()
    {
        Guid theirs = default;
        var other = await SeedCustomerAsync(Guid.NewGuid(), configure: c => theirs = AddAddress(c));
        await SignInAsNewCustomerAsync();

        await using (var context = NewContext())
            await Assert.ThrowsAsync<ValidationException>(() =>
                new DeleteAddressCommandHandler(context, CurrentUser).Handle(new DeleteAddressCommand(theirs), default));

        Assert.Single((await LoadCustomerAsync(other.Id))!.Addresses);
    }

    // ---------- Defaults ----------

    [Theory]
    [InlineData(AddressType.Shipping)]
    [InlineData(AddressType.Billing)]
    public async Task SetDefault_MovesTheDefault_AndPersists(AddressType type)
    {
        Guid home = default, office = default;
        var userId = Guid.NewGuid();
        var me = await SeedCustomerAsync(userId, configure: c =>
        {
            home = AddAddress(c, "Home");
            office = AddAddress(c, "Office");
        });
        CurrentUser.SignIn(userId);

        await using (var context = NewContext())
            await new SetDefaultAddressCommandHandler(context, CurrentUser).Handle(new SetDefaultAddressCommand(home, type), default);
        await using (var context = NewContext())
            await new SetDefaultAddressCommandHandler(context, CurrentUser).Handle(new SetDefaultAddressCommand(office, type), default);

        var saved = await LoadCustomerAsync(me.Id);
        bool IsDefault(Guid id) => saved!.Addresses.Single(a => a.Id == id) is var a &&
                                   (type == AddressType.Shipping ? a.IsDefaultShipping : a.IsDefaultBilling);
        Assert.True(IsDefault(office));
        Assert.False(IsDefault(home));
    }

    [Fact]
    public async Task SetDefaultShipping_DoesNotChangeBillingDefault()
    {
        Guid home = default, office = default;
        var userId = Guid.NewGuid();
        var me = await SeedCustomerAsync(userId, configure: c =>
        {
            home = AddAddress(c, "Home");
            office = AddAddress(c, "Office");
            c.SetDefaultBillingAddress(home);
        });
        CurrentUser.SignIn(userId);

        await using (var context = NewContext())
            await new SetDefaultAddressCommandHandler(context, CurrentUser)
                .Handle(new SetDefaultAddressCommand(office, AddressType.Shipping), default);

        var saved = await LoadCustomerAsync(me.Id);
        Assert.Equal(home, saved!.DefaultBillingAddress?.Id);
        Assert.Equal(office, saved.DefaultShippingAddress?.Id);
    }

    [Fact]
    public async Task SetDefault_AddressOfAnotherCustomer_IsRejected()
    {
        Guid theirs = default;
        await SeedCustomerAsync(Guid.NewGuid(), configure: c => theirs = AddAddress(c));
        await SignInAsNewCustomerAsync();

        await using var context = NewContext();
        await Assert.ThrowsAsync<ValidationException>(() => new SetDefaultAddressCommandHandler(context, CurrentUser)
            .Handle(new SetDefaultAddressCommand(theirs, AddressType.Shipping), default));
    }
}
