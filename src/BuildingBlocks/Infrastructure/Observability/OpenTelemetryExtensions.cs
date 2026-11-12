using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace BuildingBlocks.Infrastructure.Observability;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddOpenTelemetryTracing(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName)
    {
        var otlpEndpoint = new Uri(
            configuration["Otel:Endpoint"] ?? "http://localhost:4317");

        services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSource("Npgsql")
                .AddSource("MassTransit")
                .AddOtlpExporter(o => o.Endpoint = otlpEndpoint))
            .WithMetrics(metrics => metrics
                // .AddMeter(AiMetrics.MeterName)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddProcessInstrumentation()
                .AddMeter("Ecommerce.Ai")  
                .AddMeter("Ecommerce.*")
                .AddOtlpExporter((exporter, reader) =>
                {
                    exporter.Endpoint = otlpEndpoint;
                    reader.PeriodicExportingMetricReaderOptions
                        .ExportIntervalMilliseconds = 15000;
                }));

        return services;
    }
}