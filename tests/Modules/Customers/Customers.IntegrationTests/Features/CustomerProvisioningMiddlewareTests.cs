using System.Security.Claims;
using System.Text.Json;
using Customers.Domain.Events;
using Customers.Infrastructure;
using Customers.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Customers.IntegrationTests.Features;

public class CustomerProvisioningMiddlewareTests(PostgresFixture db) : CustomersTestBase(db)
{
    private bool _nextCalled;

    private async Task<HttpContext> InvokeAsync(string path = "/api/customers/me", params Claim[] claims)
    {
        var http = new DefaultHttpContext();
        http.Request.Path = path;
        http.Response.Body = new MemoryStream();
        http.User = new ClaimsPrincipal(new ClaimsIdentity(claims, claims.Length > 0 ? "Test" : null));

        var middleware = new CustomerProvisioningMiddleware(_ =>
        {
            _nextCalled = true;
            return Task.CompletedTask;
        });

        await using var context = NewContext();
        await middleware.InvokeAsync(http, context, CurrentUser);
        return http;
    }

    private static Claim[] KeycloakClaims(string email = "jane@shop.test", string given = "Jane", string family = "Doe") =>
    [
        new("email", email),
        new("given_name", given),
        new("family_name", family),
    ];

    private async Task<int> CountCustomersAsync()
    {
        await using var context = NewContext();
        return await context.Customers.CountAsync();
    }

    [Fact]
    public async Task Anonymous_PassesThrough_WithoutCreatingAnything()
    {
        CurrentUser.SignOut();

        await InvokeAsync();

        Assert.True(_nextCalled);
        Assert.Equal(0, await CountCustomersAsync());
    }

    [Fact]
    public async Task FirstAuthenticatedRequest_CreatesCustomerFromTokenClaims()
    {
        var userId = Guid.NewGuid();
        CurrentUser.SignIn(userId);

        await InvokeAsync(claims: KeycloakClaims("jane@shop.test", "Jane", "Doe"));

        Assert.True(_nextCalled);
        await using var context = NewContext();
        var customer = await context.Customers.SingleAsync();
        Assert.Equal(userId.ToString(), customer.IdentitySubject);
        Assert.Equal("jane@shop.test", customer.Email);
        Assert.Equal("Jane", customer.FirstName);
        Assert.Equal("Doe", customer.LastName);
        Assert.True(customer.IsActive);
    }

    [Fact]
    public async Task FirstAuthenticatedRequest_RaisesCustomerRegistered()
    {
        CurrentUser.SignIn(Guid.NewGuid());

        await InvokeAsync(claims: KeycloakClaims());

        var evt = Assert.IsType<CustomerRegisteredDomainEvent>(Assert.Single(Publisher.Published));
        Assert.Equal("jane@shop.test", evt.Email);
        Assert.Equal("Jane", evt.FirstName);
    }

    [Fact]
    public async Task TokenWithoutEmailOrName_StillProvisions_AndRegisters()
    {
        var userId = Guid.NewGuid();
        CurrentUser.SignIn(userId);

        await InvokeAsync(claims: [new Claim("preferred_username", "jane")]);

        Assert.True(_nextCalled);
        await using var context = NewContext();
        var customer = await context.Customers.SingleAsync();
        Assert.Equal(userId.ToString(), customer.IdentitySubject);
        Assert.Null(customer.Email);
        var evt = Assert.IsType<CustomerRegisteredDomainEvent>(Assert.Single(Publisher.Published));
        Assert.Null(evt.Email);
    }

    [Fact]
    public async Task LaterRequests_DoNotCreateOrRegisterAgain()
    {
        CurrentUser.SignIn(Guid.NewGuid());

        await InvokeAsync(claims: KeycloakClaims());
        await InvokeAsync(claims: KeycloakClaims());
        await InvokeAsync(claims: KeycloakClaims());

        Assert.Equal(1, await CountCustomersAsync());
        Assert.Single(Publisher.Published);
    }

    [Fact]
    public async Task ExistingCustomer_IsNotOverwrittenByTokenClaims()
    {
        var me = await SignInAsNewCustomerAsync("jane@shop.test", "Jane");

        await InvokeAsync(claims: KeycloakClaims("changed@shop.test", "Changed"));

        var saved = await LoadCustomerAsync(me.Id);
        Assert.Equal("jane@shop.test", saved!.Email);
        Assert.Equal("Jane", saved.FirstName);
    }

    [Fact]
    public async Task DeactivatedCustomer_IsBlockedWith403()
    {
        var userId = Guid.NewGuid();
        await SeedCustomerAsync(userId, configure: c => c.Deactivate());
        CurrentUser.SignIn(userId);

        var http = await InvokeAsync("/api/customers/addresses", KeycloakClaims());

        Assert.False(_nextCalled);
        Assert.Equal(StatusCodes.Status403Forbidden, http.Response.StatusCode);
        http.Response.Body.Position = 0;
        var body = await JsonDocument.ParseAsync(http.Response.Body);
        Assert.Equal("Account Deactivated", body.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task DeactivatedCustomer_CanStillReachActivate()
    {
        var userId = Guid.NewGuid();
        await SeedCustomerAsync(userId, configure: c => c.Deactivate());
        CurrentUser.SignIn(userId);

        var http = await InvokeAsync("/api/customers/me/activate", KeycloakClaims());

        Assert.True(_nextCalled);
        Assert.NotEqual(StatusCodes.Status403Forbidden, http.Response.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedWithoutUsableSubject_PassesThrough_WithoutCreatingAnything()
    {
        CurrentUser.IsAuthenticated = true;
        CurrentUser.UserId = null; // e.g. a "sub" that isn't a GUID

        await InvokeAsync(claims: KeycloakClaims());

        Assert.True(_nextCalled);
        Assert.Equal(0, await CountCustomersAsync());
    }
}
