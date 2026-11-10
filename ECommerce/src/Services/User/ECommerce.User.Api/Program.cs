using System.Net.Mail;
using ECommerce.User.Api.Authentication;
using ECommerce.User.Api.ExceptionHandling;
using ECommerce.User.Application;
using ECommerce.User.Infrastructure;
using ECommerce.User.Infrastructure.Identity;

const string ClientCors = "ClientCors";
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddCors(options => options.AddPolicy(ClientCors, policy => policy
    .WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddUserApplication();

var connectionString = builder.Configuration.GetConnectionString("UserConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Connection string 'UserConnection' was not configured.");

builder.Services.AddUserInfrastructure(connectionString);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddOptions<AdminSeedOptions>()
    .Bind(builder.Configuration.GetSection(AdminSeedOptions.SectionName))
    .Validate(options => !options.Enabled || MailAddress.TryCreate(options.Email, out _),
        "AdminSeed:Email must be a valid email address.")
    .Validate(options => !options.Enabled || !string.IsNullOrWhiteSpace(options.FirstName),
        "AdminSeed:FirstName is required when admin seeding is enabled.")
    .Validate(options => !options.Enabled || !string.IsNullOrWhiteSpace(options.LastName),
        "AdminSeed:LastName is required when admin seeding is enabled.")
    .Validate(options => !options.Enabled || options.Password.Length >= 8,
        "AdminSeed:Password must contain at least 8 characters when admin seeding is enabled.")
    .ValidateOnStart();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<IdentitySeeder>().SeedAsync();

app.UseExceptionHandler();
app.UseStatusCodePages();
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.UseRouting();
app.UseCors(ClientCors);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();

public partial class Program;
