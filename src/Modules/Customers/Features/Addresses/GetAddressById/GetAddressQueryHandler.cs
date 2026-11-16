using BuildingBlocks.Exceptions;
using BuildingBlocks.Infrastructure.Auth.CurrentUser;
using Customers.Features.Addresses.GetAddresses;
using Customers.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Customers.Features.Addresses.GetAddressById;

internal sealed class GetAddressQueryHandler(CustomersDbContext context, ICurrentUser currentUser)
    : IRequestHandler<GetAddressQuery, AddressResponse>
{
    public async Task<AddressResponse> Handle(GetAddressQuery request, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedAccessException(
                "User is not authenticated.");
        }
        var keycloakId = currentUser.UserId!.Value.ToString();

        var customer = await context.Customers
                           .AsNoTracking()
                           .Include(c => c.Addresses)
                           .FirstOrDefaultAsync(c => c.IdentitySubject == keycloakId, ct)
                       ?? throw new NotFoundException("Customer not found.");

        var address = customer.Addresses.FirstOrDefault(a => a.Id == request.AddressId)
                      ?? throw new NotFoundException("Address not found.");

        return new AddressResponse(
            address.Id,
            address.RecipientName ?? "N/A",
            address.Street,
            address.City,
            address.State,
            address.CountryCode,
            address.IsDefaultShipping,
            address.IsDefaultBilling
        );
    }
}