using Cart;
using Catalog;
using Grpc.Net.Client;
using Inventory;
using Inventory.Contracts.Grpc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace Api.IntegrationTests.Infrastructure;

/// <summary>
/// Boots the real API in memory against Postgres and RabbitMQ containers.
/// Only two things are swapped: Keycloak JWT auth (→ <see cref="TestAuthHandler"/>)
/// and the Inventory gRPC channel (→ the in-memory test server instead of localhost:5294).
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().WithImage("postgres:17").Build();
    private readonly RabbitMqContainer _rabbit = new RabbitMqBuilder().WithImage("rabbitmq:4-management").Build();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _rabbit.StartAsync());
        _ = Server; // start the host (and run migrations) once, up front
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _rabbit.DisposeAsync().AsTask());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var rabbitUri = new Uri(_rabbit.GetConnectionString());

        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _postgres.GetConnectionString());
        builder.UseSetting("RabbitMq:Host", rabbitUri.Host);
        builder.UseSetting("RabbitMq:Port", rabbitUri.Port.ToString());
        builder.UseSetting("RabbitMq:Username", RabbitMqBuilder.DefaultUsername);
        builder.UseSetting("RabbitMq:Password", RabbitMqBuilder.DefaultPassword);

        builder.ConfigureTestServices(services =>
        {
            // Migrations must run before MassTransit's hosted services start polling outbox tables
            services.Insert(0, ServiceDescriptor.Singleton<IHostedService, MigrateDatabasesHostedService>());

            services.AddAuthentication(TestAuthHandler.Scheme)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.Scheme, _ => { });
            services.PostConfigure<AuthenticationOptions>(o =>
            {
                o.DefaultScheme = TestAuthHandler.Scheme;
                o.DefaultAuthenticateScheme = TestAuthHandler.Scheme;
                o.DefaultChallengeScheme = TestAuthHandler.Scheme;
            });

            // Route Cart → Inventory gRPC calls through the in-memory server
            services.RemoveAll<InventoryGrpc.InventoryGrpcClient>();
            services.AddSingleton(_ => GrpcChannel.ForAddress(Server.BaseAddress,
                new GrpcChannelOptions { HttpHandler = Server.CreateHandler() }));
            services.AddTransient(sp => new InventoryGrpc.InventoryGrpcClient(sp.GetRequiredService<GrpcChannel>()));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Fail fast on any registration that can't be built (e.g. a missing ICatalogApi)
        builder.UseDefaultServiceProvider(o =>
        {
            o.ValidateScopes = true;
            o.ValidateOnBuild = true;
        });
        return base.CreateHost(builder);
    }

    public async Task ResetCartsAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var cartDb = (DbContext)scope.ServiceProvider.GetRequiredService(MigrateDatabasesHostedService.ContextTypes.Single(t => t.Assembly == typeof(CartModule).Assembly));
        await cartDb.Database.ExecuteSqlRawAsync("""TRUNCATE "cart"."Carts" CASCADE;""");
    }

    private sealed class MigrateDatabasesHostedService(IServiceProvider services) : IHostedService
    {
        public static readonly Type[] ContextTypes =
            new[] { typeof(CatalogModule).Assembly, typeof(InventoryModule).Assembly, typeof(CartModule).Assembly }
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(DbContext).IsAssignableFrom(t) && !t.IsAbstract)
                .ToArray();

        public async Task StartAsync(CancellationToken ct)
        {
            await using var scope = services.CreateAsyncScope();
            foreach (var type in ContextTypes)
                await ((DbContext)scope.ServiceProvider.GetRequiredService(type)).Database.MigrateAsync(ct);
        }

        public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "Api";
}
