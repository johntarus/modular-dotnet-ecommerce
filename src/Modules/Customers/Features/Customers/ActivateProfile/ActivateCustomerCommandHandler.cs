using BuildingBlocks.Exceptions;
using BuildingBlocks.Infrastructure.Auth.CurrentUser;
using Customers.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Customers.Features.Customers.ActivateProfile;

internal sealed class ActivateCustomerCommandHandler(CustomersDbContext db, ICurrentUser currentUser)
    : IRequestHandler<ActivateCustomerCommand>
{
    public async Task Handle(ActivateCustomerCommand request, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedAccessException(
                "User is not authenticated.");
        }
        var keycloakId = currentUser.UserId!.Value.ToString();
        
        var customer = await db.Customers
                           .FirstOrDefaultAsync(c => c.IdentitySubject == keycloakId, ct)
                       ?? throw new NotFoundException("Customer not found.");

        // Call the domain method we added earlier
        customer.Activate();
        
        await db.SaveChangesAsync(ct);
    }
}