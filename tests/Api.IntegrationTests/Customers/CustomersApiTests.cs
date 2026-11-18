using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Api.IntegrationTests.Infrastructure;
using Customers.Contracts.Events;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Api.IntegrationTests.Customers;

/// <summary>
/// /api/customers over HTTP: auto-provisioning on first request, profile and address endpoints,
/// exception → status mapping, deactivation gate, and the welcome email via RabbitMQ → Notifications.
/// Each test uses its own fresh user, so no reset is needed.
/// </summary>
public class CustomersApiTests(ApiFactory factory) : ApiTestBase(factory)
{
    private HttpClient Customer(out Guid userId, string? email = null, string? givenName = "Jane", string? familyName = "Doe",
        bool withEmail = true)
    {
        userId = Guid.NewGuid();
        var client = SignedIn(userId);
        if (withEmail)
            client.DefaultRequestHeaders.Add(TestAuthHandler.EmailHeader, email ?? $"{userId:N}@shop.test");
        if (givenName is not null) client.DefaultRequestHeaders.Add(TestAuthHandler.GivenNameHeader, givenName);
        if (familyName is not null) client.DefaultRequestHeaders.Add(TestAuthHandler.FamilyNameHeader, familyName);
        return client;
    }

    private static object NewAddress(string label = "Home", string country = "KE") => new
    {
        RecipientName = "Jane Doe",
        PhoneNumber = "+254700000000",
        Email = "jane@shop.test",
        Street = "1 Moi Avenue",
        City = "Nairobi",
        State = "Nairobi County",
        PostalCode = "00100",
        CountryCode = country,
        Label = label,
    };

    private static async Task<Guid> AddAddressAsync(HttpClient client, string label = "Home")
    {
        var response = await client.PostAsJsonAsync("/api/customers/addresses", NewAddress(label));
        await EnsureSuccessAsync(response);
        return (await ReadJsonAsync(response)).GetProperty("id").GetGuid();
    }

    private static async Task<JsonElement[]> GetAddressesAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/customers/addresses");
        await EnsureSuccessAsync(response);
        return (await ReadJsonAsync(response)).EnumerateArray().ToArray();
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    // ---------- Provisioning & profile ----------

    [Fact]
    public async Task Anonymous_GetProfile_Is401()
    {
        await AssertProblemAsync(await Anonymous().GetAsync("/api/customers/me"), HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task FirstRequest_ProvisionsCustomerFromToken()
    {
        var client = Customer(out var userId, email: "provisioned@shop.test", givenName: "Pat", familyName: "Lee");

        var response = await client.GetAsync("/api/customers/me");

        await EnsureSuccessAsync(response);
        var profile = await ReadJsonAsync(response);
        Assert.Equal(userId.ToString(), profile.GetProperty("identitySubject").GetString());
        Assert.Equal("provisioned@shop.test", profile.GetProperty("email").GetString());
        Assert.Equal("Pat", profile.GetProperty("firstName").GetString());
        Assert.Equal("Lee", profile.GetProperty("lastName").GetString());
    }

    [Fact]
    public async Task Registration_SendsWelcomeEmail_ThroughRabbitMqAndNotifications()
    {
        var email = $"welcome-{Guid.NewGuid():N}@shop.test";
        var client = Customer(out _, email: email, givenName: "Wanjiru");

        await EnsureSuccessAsync(await client.GetAsync("/api/customers/me"));

        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < deadline && Factory.Emails.Sent.All(m => m.To != email))
            await Task.Delay(200);

        var welcome = Assert.Single(Factory.Emails.Sent, m => m.To == email);
        Assert.Contains("Wanjiru", welcome.HtmlBody);
    }

    [Fact]
    public async Task Registration_WithoutEmail_IsConsumedCleanly_AndSendsNothing()
    {
        var observer = new RegisteredEventObserver();
        using var subscription = Factory.Services.GetRequiredService<IBus>().ConnectConsumeMessageObserver(observer);
        var client = Customer(out _, withEmail: false);

        var response = await client.GetAsync("/api/customers/me");

        await EnsureSuccessAsync(response);
        var profile = await ReadJsonAsync(response);
        Assert.Equal(JsonValueKind.Null, profile.GetProperty("email").ValueKind);
        var customerId = profile.GetProperty("id").GetGuid();

        var outcome = await observer.WaitForAsync(customerId, TimeSpan.FromSeconds(30));
        Assert.Null(outcome); // consumed without a fault (a fault would land the message in CustomerRegistered_error)
        Assert.DoesNotContain(Factory.Emails.Sent, m => string.IsNullOrEmpty(m.To));
    }

    [Fact]
    public async Task RepeatedRequests_SendOnlyOneWelcomeEmail()
    {
        var email = $"once-{Guid.NewGuid():N}@shop.test";
        var client = Customer(out _, email: email);

        for (var i = 0; i < 3; i++)
            await EnsureSuccessAsync(await client.GetAsync("/api/customers/me"));

        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < deadline && Factory.Emails.Sent.All(m => m.To != email))
            await Task.Delay(200);
        await Task.Delay(1000); // give a duplicate time to show up

        Assert.Single(Factory.Emails.Sent, m => m.To == email);
    }

    [Fact]
    public async Task UpdateProfile_Is204_AndVisibleOnGet()
    {
        var client = Customer(out _);

        var response = await client.PutAsJsonAsync("/api/customers/me",
            new { FirstName = "Janet", LastName = "Smith", PhoneNumber = "+254711111111" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var profile = await ReadJsonAsync(await client.GetAsync("/api/customers/me"));
        Assert.Equal("Janet", profile.GetProperty("firstName").GetString());
        Assert.Equal("Smith", profile.GetProperty("lastName").GetString());
        Assert.Equal("+254711111111", profile.GetProperty("phoneNumber").GetString());
    }

    [Fact]
    public async Task Deactivate_BlocksEverythingWith403_UntilActivated()
    {
        var client = Customer(out _);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/customers/me/deactivate")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/customers/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/customers/addresses")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/customers/me/activate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/customers/me")).StatusCode);
    }

    // ---------- Addresses ----------

    [Fact]
    public async Task AddAddress_Is201_WithLocationThatResolves()
    {
        var client = Customer(out _);

        var response = await client.PostAsJsonAsync("/api/customers/addresses", NewAddress());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = (await ReadJsonAsync(response)).GetProperty("id").GetGuid();
        Assert.NotNull(response.Headers.Location);
        var follow = await client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, follow.StatusCode);
        Assert.Equal(id, (await ReadJsonAsync(follow)).GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task AddAddress_InvalidCountry_Is400()
    {
        var client = Customer(out _);

        await AssertProblemAsync(
            await client.PostAsJsonAsync("/api/customers/addresses", NewAddress(country: "KEN")),
            HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAddresses_ListsOnlyMine()
    {
        var me = Customer(out _);
        var other = Customer(out _);
        var home = await AddAddressAsync(me, "Home");
        var office = await AddAddressAsync(me, "Office");
        await AddAddressAsync(other, "Theirs");

        var addresses = await GetAddressesAsync(me);

        Assert.Equal(new[] { home, office }.Order(), addresses.Select(a => a.GetProperty("id").GetGuid()).Order());
    }

    [Fact]
    public async Task GetAddress_Unknown_Is404()
    {
        var client = Customer(out _);
        await AddAddressAsync(client);

        await AssertProblemAsync(await client.GetAsync($"/api/customers/addresses/{Guid.NewGuid()}"), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAddress_OfAnotherCustomer_Is404()
    {
        var theirs = await AddAddressAsync(Customer(out _));
        var me = Customer(out _);

        await AssertProblemAsync(await me.GetAsync($"/api/customers/addresses/{theirs}"), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateAddress_Is204_AndVisibleOnGet()
    {
        var client = Customer(out _);
        var id = await AddAddressAsync(client);

        var response = await client.PutAsJsonAsync($"/api/customers/addresses/{id}", new
        {
            RecipientName = "John Doe",
            Street = "2 Kenyatta Ave",
            City = "Mombasa",
            State = "Mombasa County",
            CountryCode = "KE",
            Label = "Holiday",
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var address = await ReadJsonAsync(await client.GetAsync($"/api/customers/addresses/{id}"));
        Assert.Equal("John Doe", address.GetProperty("recipientName").GetString());
        Assert.Equal("Mombasa", address.GetProperty("city").GetString());
    }

    [Fact]
    public async Task DeleteAddress_Is204_AndItIsGone()
    {
        var client = Customer(out _);
        var home = await AddAddressAsync(client, "Home");
        var office = await AddAddressAsync(client, "Office");

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/customers/addresses/{home}")).StatusCode);

        Assert.Equal(office, Assert.Single(await GetAddressesAsync(client)).GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task DeleteAddress_OfAnotherCustomer_Is400_AndKept()
    {
        var owner = Customer(out _);
        var theirs = await AddAddressAsync(owner);

        await AssertProblemAsync(await Customer(out _).DeleteAsync($"/api/customers/addresses/{theirs}"), HttpStatusCode.BadRequest);
        Assert.Single(await GetAddressesAsync(owner));
    }

    [Fact]
    public async Task DefaultShippingAndBilling_AreSetIndependently()
    {
        var client = Customer(out _);
        var home = await AddAddressAsync(client, "Home");
        var office = await AddAddressAsync(client, "Office");

        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsync($"/api/customers/addresses/{home}/default-shipping", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsync($"/api/customers/addresses/{office}/default-billing", null)).StatusCode);

        var byId = (await GetAddressesAsync(client)).ToDictionary(a => a.GetProperty("id").GetGuid());
        Assert.True(byId[home].GetProperty("isDefaultShipping").GetBoolean());
        Assert.False(byId[home].GetProperty("isDefaultBilling").GetBoolean());
        Assert.True(byId[office].GetProperty("isDefaultBilling").GetBoolean());
        Assert.False(byId[office].GetProperty("isDefaultShipping").GetBoolean());
    }

    /// <summary>Completes once the Notifications consumer has finished with a given customer's event: null if it succeeded, the exception if it faulted.</summary>
    private sealed class RegisteredEventObserver : IConsumeMessageObserver<CustomerRegisteredIntegrationEvent>
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, TaskCompletionSource<Exception?>> _outcomes = new();

        private TaskCompletionSource<Exception?> For(Guid customerId) =>
            _outcomes.GetOrAdd(customerId, _ => new TaskCompletionSource<Exception?>(TaskCreationOptions.RunContinuationsAsynchronously));

        public async Task<Exception?> WaitForAsync(Guid customerId, TimeSpan timeout) =>
            await For(customerId).Task.WaitAsync(timeout);

        public Task PreConsume(ConsumeContext<CustomerRegisteredIntegrationEvent> context) => Task.CompletedTask;

        public Task PostConsume(ConsumeContext<CustomerRegisteredIntegrationEvent> context)
        {
            For(context.Message.CustomerId).TrySetResult(null);
            return Task.CompletedTask;
        }

        public Task ConsumeFault(ConsumeContext<CustomerRegisteredIntegrationEvent> context, Exception exception)
        {
            For(context.Message.CustomerId).TrySetResult(exception);
            return Task.CompletedTask;
        }
    }
}
