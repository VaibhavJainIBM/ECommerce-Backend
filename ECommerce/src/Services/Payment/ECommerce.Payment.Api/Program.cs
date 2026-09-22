using ECommerce.Payment.Infrastructure;

var builder =
    WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddHealthChecks();

var connectionString =
    builder.Configuration
        .GetConnectionString(
            "PaymentConnection");

if (string.IsNullOrWhiteSpace(
        connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'PaymentConnection' was not configured.");
}

builder.Services.AddPaymentInfrastructure(
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