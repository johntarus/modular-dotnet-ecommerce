namespace Customers.Contracts;

public interface ICustomersModuleApi
{
    Task<CustomerAddressDto?> GetCustomerAddressAsync(
        string identitySubject, 
        Guid addressId, 
        CancellationToken ct = default);
}

public sealed record CustomerAddressDto(
    Guid Id,
    Guid CustomerId,
    string RecipientName,
    string? PhoneNumber,
    string Street,
    string City,
    string State,
    string CountryCode,
    string PostalCode);