using BuildingBlocks.Infrastructure;
using BuildingBlocks.Infrastructure.Middleware;
using Cart;
using Catalog;
using FluentValidation;
using Inventory;
using Inventory.Features.Inventory.Grpc;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

var builder = WebApplication.CreateBuilder(args);

Serilog.Debugging.SelfLog.Enable(message => { Console.Error.WriteLine($"[SERILOG SELF-LOG] {message}"); });

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddGrpc();

builder.Services.AddBuildingBlocks(builder.Configuration, typeof(CatalogModule).Assembly,
    typeof(InventoryModule).Assembly);
builder.Services.AddValidatorsFromAssemblies(new[]
{
    typeof(CatalogModule).Assembly,
    typeof(InventoryModule).Assembly
});

builder.Services.AddCatalog(builder.Configuration);
builder.Services.AddInventory(builder.Configuration);
builder.Services.AddCart(builder.Configuration);

builder.Services.AddMessaging(builder.Configuration);

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Ecommerce API",
        Version = "v1"
    });
});

builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty(
            "Application",
            context.HostingEnvironment.ApplicationName)
        .WriteTo.Console()
        .WriteTo.OpenTelemetry(options =>
        {
            options.Endpoint = "http://otel-collector:4318";
            options.Protocol = OtlpProtocol.HttpProtobuf;

            options.ResourceAttributes = new Dictionary<string, object>
            {
                ["service.name"] = "ecommerce-api",
                ["deployment.environment"] =
                    context.HostingEnvironment.EnvironmentName
            };
        });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ecommerce API v1"));
}

app.UseMiddleware<CorrelationIdMiddleware>();

app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        var correlationId =
            httpContext.Items[CorrelationIdMiddleware.HeaderName]?.ToString()
            ?? httpContext.TraceIdentifier;

        diagnosticContext.Set("CorrelationId", correlationId);
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
    };
});

app.UseBuildingBlocks();

if (!string.IsNullOrEmpty(app.Configuration["https_port"]))
{
    app.UseHttpsRedirection();
}

app.MapGrpcService<InventoryGrpcService>();
app.MapCatalog();
app.MapInventory();
app.MapCart();

app.Run();