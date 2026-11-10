using ECommerce.Order.Api.Authentication;
using ECommerce.Order.Application;
using ECommerce.Order.Application.Abstractions;
using ECommerce.Order.Infrastructure;

const string AngularClientCors = "AngularClientCors";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        AngularClientCors,
        policy => policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddOrderApplication();
builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<HttpUserContext>();
builder.Services.AddScoped<ICurrentUser>(serviceProvider =>
    serviceProvider.GetRequiredService<HttpUserContext>());

var connectionString = builder.Configuration
    .GetConnectionString("OrderConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "OrderConnection was not configured.");
}

builder.Services.AddOrderInfrastructure(connectionString);

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

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
