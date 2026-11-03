using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BuildingBlocks.Infrastructure.Auth;

public static class AuthExtensions
{
    public static IServiceCollection AddAuth(
        this IServiceCollection services, IConfiguration config, IHostEnvironment env)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.Authority = config["Auth:Authority"];
                o.Audience = config["Auth:Audience"];
                o.RequireHttpsMetadata = !env.IsDevelopment();
                o.MapInboundClaims = false;              // keep "sub" and "roles" as-is
                o.TokenValidationParameters.NameClaimType = "sub";
                o.TokenValidationParameters.RoleClaimType = "roles";

                o.TokenValidationParameters.ValidIssuers = new[]
                {
                    "http://localhost:8080/realms/ecommerce",
                    "http://keycloak:8080/realms/ecommerce"
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthPolicies.CanManageCatalog, p => p.RequireRole("Admin"))
            .AddPolicy(AuthPolicies.CanAdjustStock, p => p.RequireRole("Admin"));

        return services;
    }
    public static IApplicationBuilder UseAuth(this IApplicationBuilder app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }
}