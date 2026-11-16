using BuildingBlocks.Exceptions;
using BuildingBlocks.Infrastructure.Auth.CurrentUser;
using Customers.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Customers.Features.Addresses.SetDefaultAddress;

internal sealed class SetDefaultAddressCommandHandler(CustomersDbContext context, ICurrentUser currentUser)
    : IRequestHandler<SetDefaultAddressCommand>
{
    public async Task Handle(SetDefaultAddressCommand request, CancellationToken ct)
    {
        var keycloakId = currentUser.UserId!.Value.ToString();
        var customer = await context.Customers
                           .Include(c => c.Addresses) // Include addresses because the domain method iterates over them
                           .FirstOrDefaultAsync(c => c.IdentitySubject == keycloakId, ct)
                       ?? throw new NotFoundException("Customer not found.");

        // Call the Domain method based on the AddressType
        if (request.Type == AddressType.Shipping)
            customer.SetDefaultShippingAddress(request.AddressId);
        else
            customer.SetDefaultBillingAddress(request.AddressId);

        await context.SaveChangesAsync(ct);
    }
}