using ECommerce.Payment.Api.Authentication;
using ECommerce.Payment.Application;
using ECommerce.Payment.Application.Abstractions;
using ECommerce.Payment.Infrastructure;

var builder =
    WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddHealthChecks();

builder.Services.AddHttpContextAccessor();

builder.Services.AddPaymentApplication();

builder.Services.AddJwtAuthentication(
    builder.Configuration);

builder.Services.AddScoped<HttpUserContext>();

builder.Services.AddScoped<ICurrentUser>(
    sp =>
        sp.GetRequiredService<HttpUserContext>());

builder.Services.AddScoped<IAccessTokenAccessor>(
    sp =>
        sp.GetRequiredService<HttpUserContext>());

var paymentConnection =
    builder.Configuration
        .GetConnectionString(
            "PaymentConnection");

if (string.IsNullOrWhiteSpace(
        paymentConnection))
{
    throw new InvalidOperationException(
        "PaymentConnection was not configured.");
}

var orderServiceUrl =
    builder.Configuration[
        "Services:Order"];

if (string.IsNullOrWhiteSpace(
        orderServiceUrl))
{
    throw new InvalidOperationException(
        "Services:Order was not configured.");
}

builder.Services.AddPaymentInfrastructure(
    paymentConnection,
    orderServiceUrl);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();

public partial class Program
{
}