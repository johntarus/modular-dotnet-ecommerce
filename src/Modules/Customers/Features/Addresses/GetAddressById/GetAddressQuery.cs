using Customers.Features.Addresses.GetAddresses;
using MediatR;

namespace Customers.Features.Addresses.GetAddressById;

public sealed record GetAddressQuery(Guid AddressId) : IRequest<AddressResponse>;