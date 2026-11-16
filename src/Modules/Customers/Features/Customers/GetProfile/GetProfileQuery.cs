using MediatR;

namespace Customers.Features.Customers.GetProfile;

public class GetProfileQuery : IRequest<ProfileResponse>;