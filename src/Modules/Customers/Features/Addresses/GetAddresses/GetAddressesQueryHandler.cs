using BuildingBlocks.Infrastructure.Auth.CurrentUser;
using Customers.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Customers.Features.Addresses.GetAddresses;

internal sealed class GetAddressesQueryHandler(
    CustomersDbContext dbContext,
    ICurrentUser currentUser
) : IRequestHandler<GetAddressesQuery, IReadOnlyList<AddressResponse>>
{
    public async Task<IReadOnlyList<AddressResponse>> Handle(
        GetAddressesQuery request,
        CancellationToken cancellationToken)
    {
        // Ensure the user is authenticated.
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedAccessException(
                "User is not authenticated.");
        }

        // Retrieve the Keycloak subject.
        var identitySubject = currentUser.UserId;

        if (string.IsNullOrWhiteSpace(identitySubject.ToString()))
        {
            throw new UnauthorizedAccessException(
                "User identity subject is missing.");
        }

        // Find the customer and retrieve their addresses.
        var customer = await dbContext.Customers
            .AsNoTracking()
            .Include(c => c.Addresses)
            .FirstOrDefaultAsync(
                c => c.IdentitySubject == identitySubject.ToString(),
                cancellationToken);

        if (customer is null)
        {
            throw new KeyNotFoundException(
                "Customer was not found.");
        }

        //  Map the addresses to response DTOs.
        return customer.Addresses
            .Select(address => new AddressResponse(
                address.Id,
                address.RecipientName ?? "N/A",
                address.Street,
                address.City,
                address.State,
                address.CountryCode,
                address.IsDefaultShipping,
                address.IsDefaultBilling
            ))
            .ToList();
    }
}