using MediatR;

namespace Customers.Features.Addresses.AddAddress;

public sealed record AddAddressCommand(
    string RecipientName,
    string PhoneNumber,
    string? Email,
    string Street,
    string City,
    string State,
    string? PostalCode,
    string CountryCode,
    string? Label
) : IRequest<Guid>;