using BuildingBlocks.Infrastructure;
using Catalog;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddBuildingBlocks(typeof(CatalogModule).Assembly);

// Register Catalog module
builder.Services.AddCatalog(builder.Configuration);
builder.Services.AddValidatorsFromAssembly(typeof(CatalogModule).Assembly);
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Ecommerce API", Version = "v1" });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c=>c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ecommerce API v1"));   
}

app.UseBuildingBlocks();
app.UseHttpsRedirection();

// Map Catalog endpoints
app.MapCatalog();

app.Run();
