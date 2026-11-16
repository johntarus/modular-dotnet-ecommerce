using MediatR;

namespace Customers.Features.Addresses.SetDefaultAddress;

// Using an enum makes the intent clearer than a bool
public enum AddressType
{
    Shipping,
    Billing
}

public sealed record SetDefaultAddressCommand(Guid AddressId, AddressType Type) : IRequest;