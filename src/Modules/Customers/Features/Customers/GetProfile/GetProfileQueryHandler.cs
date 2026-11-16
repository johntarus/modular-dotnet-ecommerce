using BuildingBlocks.Exceptions;
using BuildingBlocks.Infrastructure.Auth.CurrentUser;
using Customers.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Customers.Features.Customers.GetProfile;

internal sealed class GetProfileQueryHandler(CustomersDbContext context, ICurrentUser currentUser)
    : IRequestHandler<GetProfileQuery, ProfileResponse>
{
    public async Task<ProfileResponse> Handle(GetProfileQuery request, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedAccessException(
                "User is not authenticated.");
        }
        var keycloakId = currentUser.UserId!.Value.ToString();

        var customer = await context.Customers.AsNoTracking()
                           .FirstOrDefaultAsync(c => c.IdentitySubject == keycloakId, ct) ??
                       throw new NotFoundException("Customer not Found");

        return new ProfileResponse(customer.Id, customer.IdentitySubject, customer.FirstName, customer.LastName,
            customer.Email, customer.PhoneNumber);
    }
}