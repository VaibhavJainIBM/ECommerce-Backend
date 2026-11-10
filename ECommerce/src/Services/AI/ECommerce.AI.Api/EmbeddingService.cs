using OpenAI.Embeddings;

namespace ECommerce.AI.Api;

public sealed class EmbeddingService(EmbeddingClient client)
{
    public async Task<float[]> CreateAsync(
        string text,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException(
                "Embedding text cannot be empty.",
                nameof(text));
        }

        OpenAIEmbedding embedding = await client.GenerateEmbeddingAsync(
            text,
            cancellationToken: cancellationToken);

        return embedding.ToFloats().ToArray();
    }
}
