using BuildingBlocks.Exceptions;
using BuildingBlocks.Infrastructure.Auth.CurrentUser;
using Customers.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Customers.Features.Addresses.AddAddress;

internal class AddAddressCommandHandler(CustomersDbContext context, ICurrentUser currentUser) : IRequestHandler<AddAddressCommand, Guid>
{
    public async Task<Guid> Handle(AddAddressCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId;

        if (userId is null)
        {
            throw new UnauthorizedAccessException(
                "Authenticated user does not have a valid identity subject.");
        }

        var keycloakId = userId.Value.ToString();
        var customer = await context.Customers.FirstOrDefaultAsync(c => c.IdentitySubject == keycloakId, ct)
                       ?? throw new NotFoundException("Customer not found.");

        var addressId = customer.AddAddress(
            customer.Id, request.RecipientName, request.PhoneNumber, request.Email, 
            request.Street, request.City, request.State, request.PostalCode, 
            request.CountryCode, request.Label);

        await context.SaveChangesAsync(ct);
        return addressId;
    }
}