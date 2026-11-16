using MediatR;

namespace Customers.Features.Customers.UpdateProfile;

public sealed record UpdateProfileCommand(
    string FirstName,
    string LastName,
    string? PhoneNumber
) : IRequest<Guid>;