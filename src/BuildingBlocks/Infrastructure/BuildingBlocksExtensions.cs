using BuildingBlocks.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using BuildingBlocks.Infrastructure.CurrentUser;
using BuildingBlocks.Infrastructure.Observability;

namespace BuildingBlocks.Infrastructure;

public static class BuildingBlocksExtensions
{
    public static IServiceCollection AddBuildingBlocks(
        this IServiceCollection services,
        params Assembly[] moduleAssemblies)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser.CurrentUser>();
        
        services.AddProblemDetails();
        
        services.AddOpenTelemetryTracing("Api");

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(moduleAssemblies);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        foreach (var assembly in moduleAssemblies)
            services.AddValidatorsFromAssembly(assembly);

        return services;
    }

    public static IApplicationBuilder UseBuildingBlocks(this IApplicationBuilder app)
    {
        app.UseExceptionHandler();
        return app;
    }
}
