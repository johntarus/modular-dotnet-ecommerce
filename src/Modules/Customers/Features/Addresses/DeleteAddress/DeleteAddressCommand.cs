using MediatR;

namespace Customers.Features.Addresses.DeleteAddress;

public sealed record DeleteAddressCommand(Guid AddressId) : IRequest;