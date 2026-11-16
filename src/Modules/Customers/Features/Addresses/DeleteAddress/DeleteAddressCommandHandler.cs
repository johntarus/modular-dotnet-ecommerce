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
        var keycloakId = currentUser.UserId!.Value.ToString();
        var customer = await context.Customers.FirstOrDefaultAsync(c => c.IdentitySubject == keycloakId, ct)
                       ?? throw new NotFoundException("Customer not found.");

        // Call the Domain method
        customer.RemoveAddress(request.AddressId);
        
        await context.SaveChangesAsync(ct);
    }
}