using BuildingBlocks.Exceptions;
using BuildingBlocks.Infrastructure.Auth.CurrentUser;
using Customers.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Customers.Features.Addresses.UpdateAddress;

internal sealed class UpdateAddressCommandHandler(CustomersDbContext context, ICurrentUser currentUser)
    : IRequestHandler<UpdateAddressCommand>
{
    public async Task Handle(UpdateAddressCommand request, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }
        
        var keycloakId = currentUser.UserId.Value.ToString();
        
        // Load the Aggregate Root (Customer)
        var customer = await context.Customers
                           .Include(c => c.Addresses) // UpdateAddress looks the address up in this collection
                           .FirstOrDefaultAsync(c => c.IdentitySubject == keycloakId, ct)
                       ?? throw new NotFoundException("Customer not found.");

        // Call the Domain method directly on the request
        customer.UpdateAddress(
            request.AddressId,
            request.RecipientName, 
            request.PhoneNumber, 
            request.Email,
            request.Street, 
            request.City, 
            request.State, 
            request.PostalCode, 
            request.CountryCode, 
            request.Label);

        await context.SaveChangesAsync(ct);
    }
}