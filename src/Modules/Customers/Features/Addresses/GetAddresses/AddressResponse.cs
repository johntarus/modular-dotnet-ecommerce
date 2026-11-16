namespace Customers.Features.Addresses.GetAddresses;

public record AddressResponse(Guid Id,
    string RecipientName,
    string Street,
    string City,
    string State,
    string CountryCode,
    bool IsDefaultShipping,
    bool IsDefaultBilling);