using BuildingBlocks.Exceptions;
using BuildingBlocks.Infrastructure.Auth.CurrentUser;
using Customers.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Customers.Features.Customers.UpdateProfile;

internal class UpdateProfileCommandHandler(CustomersDbContext context, ICurrentUser currentUser) : IRequestHandler<UpdateProfileCommand, Guid>
{
    public async Task<Guid> Handle(UpdateProfileCommand request, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedAccessException(
                "User is not authenticated.");
        }
        var keycloakId = currentUser.UserId!.Value.ToString();
        var customer = await context.Customers.FirstOrDefaultAsync(c => c.IdentitySubject == keycloakId, ct)
                       ?? throw new NotFoundException("Customer not found.");

        customer.UpdateProfile(request.FirstName, request.LastName, customer.Email, request.PhoneNumber);
        await context.SaveChangesAsync(ct);
        return customer.Id;
    }
}