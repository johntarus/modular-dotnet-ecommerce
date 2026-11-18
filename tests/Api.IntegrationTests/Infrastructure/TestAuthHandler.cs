using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Api.IntegrationTests.Infrastructure;

/// <summary>
/// Replaces Keycloak JWT validation. A request carrying "X-Test-User: {guid}" is signed in
/// as that user (claim "sub", like Keycloak); "X-Test-Roles: Admin,Other" adds role claims;
/// "X-Test-Email" / "X-Test-Given-Name" / "X-Test-Family-Name" add the matching Keycloak profile claims.
/// Without the user header the request is anonymous.
/// </summary>
public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string Scheme = "Test";
    public const string UserHeader = "X-Test-User";
    public const string RolesHeader = "X-Test-Roles";
    public const string EmailHeader = "X-Test-Email";
    public const string GivenNameHeader = "X-Test-Given-Name";
    public const string FamilyNameHeader = "X-Test-Family-Name";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userId = Request.Headers[UserHeader].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(userId))
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new List<Claim> { new("sub", userId) };
        foreach (var (header, claim) in new[] { (EmailHeader, "email"), (GivenNameHeader, "given_name"), (FamilyNameHeader, "family_name") })
            if (Request.Headers[header].FirstOrDefault() is { Length: > 0 } value)
                claims.Add(new Claim(claim, value));

        var roles = Request.Headers[RolesHeader].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(roles))
            claims.AddRange(roles.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(role => new Claim("roles", role)));

        var identity = new ClaimsIdentity(claims, Scheme, "sub", "roles");
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme)));
    }
}
