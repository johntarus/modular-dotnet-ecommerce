using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

namespace BuildingBlocks.Logging;

public static class SerilogExtensions
{
    public static IHostBuilder AddCustomSerilog(this IHostBuilder host)
    {
        host.UseSerilog((context, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", context.HostingEnvironment.ApplicationName)
                .WriteTo.Console()
                .WriteTo.OpenTelemetry(options =>
                {
                    options.Endpoint = "http://otel-collector:4318";
                    options.Protocol = OtlpProtocol.HttpProtobuf;
                    options.ResourceAttributes = new Dictionary<string, object>
                    {
                        ["service.name"] = "ecommerce-api",
                        ["deployment.environment"] = context.HostingEnvironment.EnvironmentName
                    };
                });
        });
        return host;
    }
}