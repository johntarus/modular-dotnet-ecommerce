using MediatR;

namespace Customers.Features.Addresses.GetAddresses;

public class GetAddressesQuery : IRequest<IReadOnlyList<AddressResponse>>;