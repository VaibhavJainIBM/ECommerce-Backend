using System.ClientModel;
using System.Threading.RateLimiting;
using ECommerce.AI.Api;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;

const string AngularClientCors = "AngularClientCors";

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile(
    "appsettings.Local.json",
    optional: true,
    reloadOnChange: true);

builder.Services.Configure<FoundryOptions>(
    builder.Configuration.GetSection(FoundryOptions.SectionName));

builder.Services.Configure<DownstreamOptions>(
    builder.Configuration.GetSection(DownstreamOptions.SectionName));

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddHttpContextAccessor();

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        AngularClientCors,
        policy =>
        {
            policy
                .WithOrigins("http://localhost:4200")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter(
        "ai-api",
        limiter =>
        {
            limiter.PermitLimit = 20;
            limiter.Window = TimeSpan.FromMinutes(1);
            limiter.QueueLimit = 0;
            limiter.AutoReplenishment = true;
        });
});

builder.Services.AddSingleton(sp =>
{
    var options = sp
        .GetRequiredService<IOptions<FoundryOptions>>()
        .Value;

    ValidateFoundryOptions(options);

    return new ChatClient(
        model: options.ChatDeployment,
        credential: new ApiKeyCredential(options.ApiKey),
        options: new OpenAIClientOptions
        {
            Endpoint = new Uri(options.OpenAiEndpoint)
        });
});

builder.Services.AddSingleton(sp =>
{
    var options = sp
        .GetRequiredService<IOptions<FoundryOptions>>()
        .Value;

    ValidateFoundryOptions(options);

    return new EmbeddingClient(
        model: options.EmbeddingDeployment,
        credential: new ApiKeyCredential(options.ApiKey),
        options: new OpenAIClientOptions
        {
            Endpoint = new Uri(options.OpenAiEndpoint)
        });
});

builder.Services.AddSingleton<EmbeddingService>();
builder.Services.AddSingleton<InMemoryProductIndex>();
builder.Services.AddScoped<CommerceChatService>();

builder.Services.AddHttpClient<StorefrontCatalogClient>(
    (sp, client) =>
    {
        var options = sp
            .GetRequiredService<IOptions<DownstreamOptions>>()
            .Value;

        client.BaseAddress = new Uri(options.CommerceApiBaseUrl);
    });

builder.Services.AddHttpClient<CustomerCommerceContextClient>(
    (sp, client) =>
    {
        var options = sp
            .GetRequiredService<IOptions<DownstreamOptions>>()
            .Value;

        client.BaseAddress = new Uri(options.CommerceApiBaseUrl);
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapPost(
        "/api/ai/admin/reindex",
        async (
            InMemoryProductIndex index,
            CancellationToken cancellationToken) =>
        {
            var count = await index.ReindexAsync(cancellationToken);
            return Results.Ok(new { indexedListings = count });
        });
}

app.UseCors(AngularClientCors);
app.UseRateLimiter();

app.MapPost(
        "/api/ai/chat",
        async (
            ChatRequest request,
            CommerceChatService chat,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Message) ||
                request.Message.Length > 2000)
            {
                return Results.BadRequest(new
                {
                    message = "Message must contain 1 to 2000 characters."
                });
            }

            var result = await chat.ReplyAsync(request, cancellationToken);

            return result.AuthenticationFailed
                ? Results.Unauthorized()
                : Results.Ok(result.Response);
        })
    .RequireRateLimiting("ai-api");

app.MapPost(
        "/api/ai/recommendations",
        async (
            RecommendationRequest request,
            InMemoryProductIndex index,
            CustomerCommerceContextClient commerceContextClient,
            CancellationToken cancellationToken) =>
        {
            var context = await commerceContextClient.LoadAsync(
                request.Query ?? string.Empty,
                cancellationToken);

            if (context.AuthenticationFailed)
            {
                return Results.Unauthorized();
            }

            var preferences =
                CustomerCommerceContextClient.BuildPreferenceText(context);

            var queryParts = new[]
                {
                    request.Query,
                    string.IsNullOrWhiteSpace(preferences)
                        ? null
                        : $"Customer preferences: {preferences}"
                }
                .Where(value => !string.IsNullOrWhiteSpace(value));

            var query = string.Join("\n", queryParts);

            var excluded = new HashSet<string>(
                request.ExcludeListingIds ?? [],
                StringComparer.OrdinalIgnoreCase);

            foreach (var item in context.Cart?.Items ?? [])
            {
                excluded.Add(item.ListingId.ToString());
            }

            var results = await index.SearchAsync(
                query,
                request.Take,
                excluded,
                cancellationToken);

            return Results.Ok(results);
        })
    .RequireRateLimiting("ai-api");

app.MapHealthChecks("/health");

app.Run();

static void ValidateFoundryOptions(FoundryOptions options)
{
    if (string.IsNullOrWhiteSpace(options.OpenAiEndpoint) ||
        string.IsNullOrWhiteSpace(options.ApiKey) ||
        string.IsNullOrWhiteSpace(options.ChatDeployment) ||
        string.IsNullOrWhiteSpace(options.EmbeddingDeployment))
    {
        throw new InvalidOperationException(
            "Microsoft Foundry configuration is missing. " +
            "Copy appsettings.Local.example.json to " +
            "appsettings.Local.json and replace its placeholders.");
    }
}
