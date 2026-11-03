using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace BuildingBlocks.Swagger;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerDocumentation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Swagger UI runs in the browser, so it must use the browser-reachable
        // Keycloak URL, not the Docker-internal one used for backchannel calls.
        var authority = configuration["Auth:PublicAuthority"]
            ?? configuration["Auth:Authority"]
            ?? throw new InvalidOperationException(
                "Auth:PublicAuthority / Auth:Authority is not configured.");

        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Ecommerce API",
                Version = "v1"
            });

            // OAuth2 Authorization Code flow with PKCE.
            options.AddSecurityDefinition("Keycloak", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Description = "Authenticate using Keycloak.",
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri(
                            $"{authority.TrimEnd('/')}/protocol/openid-connect/auth"),

                        TokenUrl = new Uri(
                            $"{authority.TrimEnd('/')}/protocol/openid-connect/token"),

                        Scopes = new Dictionary<string, string>
                        {
                            ["openid"] = "Authenticate users",
                            ["profile"] = "Read user profile",
                            ["email"] = "Read user email"
                        }
                    }
                }
            });

            // Apply OAuth2 authorization globally.
            options.AddSecurityRequirement(document =>
                new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Keycloak", document)] =
                        ["openid", "profile", "email"]
                });
        });

        return services;
    }

    public static IApplicationBuilder UseSwaggerDocumentation(
        this IApplicationBuilder app)
    {
        app.UseSwagger();

        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint(
                "/swagger/v1/swagger.json",
                "Ecommerce API v1");

            // Must match the public Keycloak client.
            options.OAuthClientId("ecommerce-web");

            // Authorization Code + PKCE.
            options.OAuthUsePkce();
        });

        return app;
    }
}
