using ECommerce.Product.Api.Authentication;
using ECommerce.Product.Application;
using ECommerce.Product.Infrastructure;

const string AngularClientCors =
    "AngularClientCors";

var builder =
    WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        AngularClientCors,
        policy =>
        {
            policy
                .WithOrigins(
                    "http://localhost:4200")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

builder.Services.AddProductApplication();

builder.Services.AddJwtAuthentication(
    builder.Configuration);

var connectionString =
    builder.Configuration
        .GetConnectionString(
            "ProductConnection");

if (string.IsNullOrWhiteSpace(connectionString))
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

app.UseRouting();

app.UseCors(AngularClientCors);

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();

public partial class Program
{
}