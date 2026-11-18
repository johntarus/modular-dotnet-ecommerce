using BuildingBlocks.Exceptions;
using BuildingBlocks.Infrastructure.Auth.CurrentUser;
using Customers.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Customers.Features.Addresses.DeleteAddress;

internal sealed class DeleteAddressCommandHandler(CustomersDbContext context, ICurrentUser currentUser)
    : IRequestHandler<DeleteAddressCommand>
{
    public async Task Handle(DeleteAddressCommand request, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var keycloakId = currentUser.UserId.Value.ToString();
        var customer = await context.Customers
                           .Include(c => c.Addresses) // RemoveAddress looks the address up in this collection
                           .FirstOrDefaultAsync(c => c.IdentitySubject == keycloakId, ct)
                       ?? throw new NotFoundException("Customer not found.");

        // Call the Domain method
        customer.RemoveAddress(request.AddressId);
        
        await context.SaveChangesAsync(ct);
    }
}