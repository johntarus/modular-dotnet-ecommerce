using Customers.Features.Customers.ActivateProfile;
using Customers.Features.Customers.DeactivateProfile;
using Customers.Features.Customers.GetProfile;
using Customers.Features.Customers.UpdateProfile;
using Customers.IntegrationTests.Infrastructure;

namespace Customers.IntegrationTests.Features;

public class ProfileTests(PostgresFixture db) : CustomersTestBase(db)
{
    [Fact]
    public async Task GetProfile_ReturnsTheSignedInCustomer()
    {
        await SeedCustomerAsync(Guid.NewGuid(), "other@shop.test", "Other");
        var me = await SignInAsNewCustomerAsync("jane@shop.test", "Jane");

        await using var context = NewContext();
        var profile = await new GetProfileQueryHandler(context, CurrentUser).Handle(new GetProfileQuery(), default);

        Assert.Equal(me.Id, profile.Id);
        Assert.Equal(CurrentUser.UserId.ToString(), profile.IdentitySubject);
        Assert.Equal("jane@shop.test", profile.Email);
        Assert.Equal("Jane", profile.FirstName);
        Assert.Equal("Doe", profile.LastName);
        Assert.Null(profile.PhoneNumber);
    }

    [Fact]
    public async Task UpdateProfile_PersistsNamesAndPhone_ButKeepsEmail()
    {
        var me = await SignInAsNewCustomerAsync("jane@shop.test", "Jane");

        await using (var context = NewContext())
        {
            var id = await new UpdateProfileCommandHandler(context, CurrentUser)
                .Handle(new UpdateProfileCommand(" Janet ", "Smith", "+254711111111"), default);
            Assert.Equal(me.Id, id);
        }

        var saved = await LoadCustomerAsync(me.Id);
        Assert.Equal("Janet", saved!.FirstName);
        Assert.Equal("Smith", saved.LastName);
        Assert.Equal("+254711111111", saved.PhoneNumber);
        Assert.Equal("jane@shop.test", saved.Email); // email comes from Keycloak, not this endpoint
        Assert.True(saved.UpdatedAt > me.UpdatedAt);
    }

    [Fact]
    public async Task UpdateProfile_OnlyTouchesTheSignedInCustomer()
    {
        var other = await SeedCustomerAsync(Guid.NewGuid(), "other@shop.test", "Other");
        await SignInAsNewCustomerAsync();

        await using (var context = NewContext())
            await new UpdateProfileCommandHandler(context, CurrentUser).Handle(new UpdateProfileCommand("X", "Y", null), default);

        var untouched = await LoadCustomerAsync(other.Id);
        Assert.Equal("Other", untouched!.FirstName);
    }

    [Fact]
    public async Task Deactivate_ThenActivate_IsPersisted()
    {
        var me = await SignInAsNewCustomerAsync();

        await using (var context = NewContext())
            await new DeactivateCustomerCommandHandler(context, CurrentUser).Handle(new DeactivateCustomerCommand(), default);
        Assert.False((await LoadCustomerAsync(me.Id))!.IsActive);

        await using (var context = NewContext())
            await new ActivateCustomerCommandHandler(context, CurrentUser).Handle(new ActivateCustomerCommand(), default);
        Assert.True((await LoadCustomerAsync(me.Id))!.IsActive);
    }

    [Fact]
    public async Task ProfileChanges_DoNotRaiseAnotherRegisteredEvent()
    {
        await SignInAsNewCustomerAsync();

        await using (var context = NewContext())
        {
            await new UpdateProfileCommandHandler(context, CurrentUser).Handle(new UpdateProfileCommand("A", "B", null), default);
            await new DeactivateCustomerCommandHandler(context, CurrentUser).Handle(new DeactivateCustomerCommand(), default);
        }

        Assert.Empty(Publisher.Published);
    }
}
