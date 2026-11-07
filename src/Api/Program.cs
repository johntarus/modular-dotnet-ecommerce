using Ai;
using BuildingBlocks.Infrastructure;
using BuildingBlocks.Infrastructure.Auth;
using BuildingBlocks.Infrastructure.Middleware;
using BuildingBlocks.Logging;
using BuildingBlocks.Swagger;
using Cart;
using Catalog;
using Inventory;
using Inventory.Features.Inventory.Grpc;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Serilog.Debugging.SelfLog.Enable(Console.Error.WriteLine);

// ---- Infrastructure ----
// builder.Services.AddOpenApi(); <--- REMOVE THIS
builder.Services.AddGrpc();

builder.Services.AddBuildingBlocks(builder.Configuration,
    typeof(CatalogModule).Assembly,
    typeof(InventoryModule).Assembly,
    typeof(CartModule).Assembly,
    typeof(AiModule).Assembly);

builder.Services.AddAuth(builder.Configuration, builder.Environment);
builder.Services.AddSwaggerDocumentation(builder.Configuration);

builder.Host.AddCustomSerilog();

// ---- Modules ----
builder.Services.AddCatalog(builder.Configuration);
builder.Services.AddInventory(builder.Configuration);
builder.Services.AddCart(builder.Configuration);
builder.Services.AddAiModule(builder.Configuration);

builder.Services.AddMessaging(builder.Configuration);

var app = builder.Build();

// ---- Pipeline ----
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerDocumentation();
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();
app.UseBuildingBlocks();

app.UseAuth();

if (!string.IsNullOrEmpty(app.Configuration["https_port"]))
{
    app.UseHttpsRedirection();
}

// ---- Endpoints ----
app.MapGrpcService<InventoryGrpcService>();
app.MapCatalog();
app.MapInventory();
app.MapCart();

app.Run();