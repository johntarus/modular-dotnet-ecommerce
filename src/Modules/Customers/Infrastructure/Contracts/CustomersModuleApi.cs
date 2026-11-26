using Customers.Contracts;
using Customers.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Customers.Infrastructure.Contracts;

public class CustomersModuleApi(CustomersDbContext context) : ICustomersModuleApi
{
    public async Task<CustomerAddressDto?> GetCustomerAddressAsync(string identitySubject, Guid addressId, CancellationToken ct = default)
    {
        return await context.Customers.AsNoTracking()
            .Where(c => c.IdentitySubject == identitySubject)
            .SelectMany(c => c.Addresses)
            .Where(a => a.Id == addressId)
            .Select(a => new CustomerAddressDto(
                a.Id,
                a.CustomerId,
                a.RecipientName ?? "N/A",
                a.PhoneNumber ?? null,
                a.Street,
                a.City,
                a.State,
                a.CountryCode,
                a.PostalCode ?? ""
            ))
            .FirstOrDefaultAsync(ct);
    }
}