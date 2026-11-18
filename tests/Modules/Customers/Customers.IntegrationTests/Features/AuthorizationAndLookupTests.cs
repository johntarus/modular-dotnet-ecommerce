using BuildingBlocks.Exceptions;
using BuildingBlocks.Infrastructure.Auth.CurrentUser;
using Customers.Features.Addresses.AddAddress;
using Customers.Features.Addresses.DeleteAddress;
using Customers.Features.Addresses.GetAddressById;
using Customers.Features.Addresses.GetAddresses;
using Customers.Features.Addresses.SetDefaultAddress;
using Customers.Features.Addresses.UpdateAddress;
using Customers.Features.Customers.ActivateProfile;
using Customers.Features.Customers.DeactivateProfile;
using Customers.Features.Customers.GetProfile;
using Customers.Features.Customers.UpdateProfile;
using Customers.Infrastructure.Persistence;
using Customers.IntegrationTests.Infrastructure;

namespace Customers.IntegrationTests.Features;

/// <summary>
/// Rules every Customers handler must share: a signed-out caller is rejected with
/// UnauthorizedAccessException (→ 401), and a signed-in user with no customer row gets
/// NotFoundException (→ 404) rather than a crash or an unmapped exception (→ 500).
/// </summary>
public class AuthorizationAndLookupTests(PostgresFixture db) : CustomersTestBase(db)
{
    private static readonly Dictionary<string, Func<CustomersDbContext, ICurrentUser, Task>> Handlers = new()
    {
        ["GetProfile"] = (c, u) => new GetProfileQueryHandler(c, u).Handle(new GetProfileQuery(), default),
        ["UpdateProfile"] = (c, u) => new UpdateProfileCommandHandler(c, u).Handle(new UpdateProfileCommand("A", "B", null), default),
        ["Activate"] = (c, u) => new ActivateCustomerCommandHandler(c, u).Handle(new ActivateCustomerCommand(), default),
        ["Deactivate"] = (c, u) => new DeactivateCustomerCommandHandler(c, u).Handle(new DeactivateCustomerCommand(), default),
        ["GetAddresses"] = (c, u) => new GetAddressesQueryHandler(c, u).Handle(new GetAddressesQuery(), default),
        ["GetAddress"] = (c, u) => new GetAddressQueryHandler(c, u).Handle(new GetAddressQuery(Guid.NewGuid()), default),
        ["AddAddress"] = (c, u) => new AddAddressCommandHandler(c, u).Handle(
            new AddAddressCommand("Jane", null, null, "1 Moi Ave", "Nairobi", "Nairobi", null, "KE", null), default),
        ["UpdateAddress"] = (c, u) => new UpdateAddressCommandHandler(c, u).Handle(
            new UpdateAddressCommand(Guid.NewGuid(), "Jane", null, null, "1 Moi Ave", "Nairobi", "Nairobi", null, "KE", null), default),
        ["DeleteAddress"] = (c, u) => new DeleteAddressCommandHandler(c, u).Handle(new DeleteAddressCommand(Guid.NewGuid()), default),
        ["SetDefaultAddress"] = (c, u) => new SetDefaultAddressCommandHandler(c, u).Handle(
            new SetDefaultAddressCommand(Guid.NewGuid(), AddressType.Shipping), default),
    };

    public static TheoryData<string> HandlerNames => new(Handlers.Keys);

    [Theory]
    [MemberData(nameof(HandlerNames))]
    public async Task SignedOut_IsRejectedAsUnauthorized(string handler)
    {
        CurrentUser.SignOut();
        await using var context = NewContext();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Handlers[handler](context, CurrentUser));
    }

    [Theory]
    [MemberData(nameof(HandlerNames))]
    public async Task SignedIn_WithoutCustomerRecord_IsNotFound(string handler)
    {
        await SeedCustomerAsync(Guid.NewGuid()); // someone else exists
        CurrentUser.SignIn(Guid.NewGuid());
        await using var context = NewContext();

        await Assert.ThrowsAsync<NotFoundException>(() => Handlers[handler](context, CurrentUser));
    }
}
