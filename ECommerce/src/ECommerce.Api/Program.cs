using System.Net.Mail;
using ECommerce.Infrastructure.Identity;
using ECommerce.Api.Authentication;
using ECommerce.Api.ExceptionHandling;
using ECommerce.Application;
using ECommerce.Infrastructure;
using ECommerce.Api.Authorization;
using ECommerce.Api.BackgroundJobs;
using ECommerce.Application.Payments;

const string AngularClientCors = "AngularClientCors";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

// Allow the Angular development application to call this API.
builder.Services.AddCors(options =>
{
    options.AddPolicy(AngularClientCors, policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] =
            context.HttpContext.TraceIdentifier;
    };
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddApplication();

// Configuration alone cannot enable the simulation
// in Staging or Production.
builder.Services.AddSingleton(
    new DemoPaymentMode(
        builder.Environment.IsDevelopment() &&
        builder.Configuration.GetValue<bool>(
            "DemoPayments:Enabled")));

var connectionString =
    builder.Configuration.GetConnectionString(
        "DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not configured.");
}

builder.Services.AddInfrastructure(connectionString);

builder.Services.AddJwtAuthentication(
    builder.Configuration);

builder.Services
    .AddOptions<AdminSeedOptions>()
    .Bind(
        builder.Configuration.GetSection(
            AdminSeedOptions.SectionName))
    .Validate(
        options =>
            !options.Enabled ||
            MailAddress.TryCreate(
                options.Email,
                out _),
        "AdminSeed:Email must be a valid email address.")
    .Validate(
        options =>
            !options.Enabled ||
            !string.IsNullOrWhiteSpace(
                options.FirstName),
        "AdminSeed:FirstName is required.")
    .Validate(
        options =>
            !options.Enabled ||
            !string.IsNullOrWhiteSpace(
                options.LastName),
        "AdminSeed:LastName is required.")
    .Validate(
        options =>
            !options.Enabled ||
            (!string.IsNullOrWhiteSpace(
                 options.Password) &&
             options.Password.Length >= 8),
        "AdminSeed:Password is required and must " +
        "contain at least 8 characters.")
    .ValidateOnStart();

builder.Services.AddSellerAuthorization();

builder.Services.AddHostedService<OrderExpirationWorker>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var identitySeeder =
        scope.ServiceProvider
            .GetRequiredService<IdentitySeeder>();

    await identitySeeder.SeedAsync();
}

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Determine which endpoint matches the request.
app.UseRouting();

// CORS must run before authentication and authorization.
app.UseCors(AngularClientCors);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program
{
}