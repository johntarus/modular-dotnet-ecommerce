using MediatR;

namespace Customers.Features.Addresses.UpdateAddress;

public sealed record UpdateAddressCommand(
    Guid AddressId,
    string RecipientName,
    string? PhoneNumber,
    string? Email,
    string Street,
    string City,
    string State,
    string? PostalCode,
    string CountryCode,
    string? Label) : IRequest;