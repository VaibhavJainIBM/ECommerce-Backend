namespace ECommerce.AI.Api;

public sealed class FoundryOptions
{
    public const string SectionName = "Foundry";

    public string OpenAiEndpoint { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public string ChatDeployment { get; init; } = "gpt-4.1-mini";
    public string EmbeddingDeployment { get; init; } =
        "text-embedding-3-small";
}

public sealed class DownstreamOptions
{
    public const string SectionName = "Downstream";

    public string CommerceApiBaseUrl { get; init; } = "http://localhost:5167/";
}
