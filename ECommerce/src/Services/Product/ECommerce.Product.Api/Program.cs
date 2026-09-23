using ECommerce.Product.Application;
using ECommerce.Product.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddHealthChecks();

builder.Services.AddProductApplication();

var connectionString =
    builder.Configuration
        .GetConnectionString(
            "ProductConnection");

if (string.IsNullOrWhiteSpace(
        connectionString))
{
    throw new InvalidOperationException(
        "ProductConnection was not configured.");
}

builder.Services.AddProductInfrastructure(
    connectionString);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();

public partial class Program
{
}