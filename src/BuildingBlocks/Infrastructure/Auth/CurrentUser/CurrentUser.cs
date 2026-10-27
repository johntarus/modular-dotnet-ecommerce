using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace BuildingBlocks.Infrastructure.CurrentUser;

internal sealed class CurrentUser(IHttpContextAccessor httpContextAccessor, IHostEnvironment env)
    : ICurrentUser
{
    private const string DebugHeader = "X-Debug-User";

    public Guid? UserId
    {
        get
        {
            var http = httpContextAccessor.HttpContext;
            if (http is null) return null;

            // Real identity always wins
            var claim = http.User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? http.User.FindFirstValue("sub");
            if (Guid.TryParse(claim, out var userId)) return userId;

            // Development only: act as a user without auth
            if (env.IsDevelopment() &&
                Guid.TryParse(http.Request.Headers[DebugHeader].FirstOrDefault(), out var debugId))
                return debugId;

            return null;
        }
    }
}