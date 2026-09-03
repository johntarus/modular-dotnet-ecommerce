using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Infrastructure.CurrentUser;

public class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public string? UserId => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
}