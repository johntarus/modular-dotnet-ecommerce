using BuildingBlocks.Exceptions;
using BuildingBlocks.Infrastructure.Auth.CurrentUser;
using Customers.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Customers.Features.Customers.DeactivateProfile;

internal sealed class DeactivateCustomerCommandHandler(CustomersDbContext context, ICurrentUser currentUser)
    : IRequestHandler<DeactivateCustomerCommand>
{
    public async Task Handle(DeactivateCustomerCommand request, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedAccessException(
                "User is not authenticated.");
        }
        var keycloakId = currentUser.UserId!.Value.ToString();
        
        var customer = await context.Customers
                           .FirstOrDefaultAsync(c => c.IdentitySubject == keycloakId, ct)
                       ?? throw new NotFoundException("Customer not found.");

        customer.Deactivate();
        await context.SaveChangesAsync(ct);
    }
}