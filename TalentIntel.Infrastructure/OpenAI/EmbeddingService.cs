using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Embeddings;
using TalentIntel.Application.Interfaces;

namespace TalentIntel.Infrastructure.OpenAI;

/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EmbeddingService"/> class.
/// </remarks>
public class EmbeddingService(
    AzureOpenAIClient openAIClient,
    IConfiguration configuration,
    ILogger<EmbeddingService> logger) : IEmbeddingService
{
    private readonly AzureOpenAIClient _openAIClient = openAIClient;
    private readonly string _deploymentName = configuration["Azure:OpenAI:DeploymentName"] ?? string.Empty;
    private readonly ILogger<EmbeddingService> _logger = logger;
    private const int MaxCharacters = 24000;

    /// <summary>
    /// Calls Azure OpenAI text-embedding-3-small and returns a 1536-d float vector.
    /// </summary>
    public async Task<List<float>> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating embedding with deployment {Deployment}", _deploymentName);

        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        // Truncate input to keep token usage within service limits.
        var normalizedText = TruncateToTokenLimit(text);

        var embeddingClient = _openAIClient.GetEmbeddingClient(_deploymentName);
        var options = new EmbeddingGenerationOptions();

        var response = await embeddingClient.GenerateEmbeddingAsync(normalizedText, options, cancellationToken);
        // Azure OpenAI returns a single embedding vector for the supplied input.
        var embedding = response.Value.Vector.ToArray().ToList();

        _logger.LogInformation(
            "Generated embedding with deployment {Deployment} for {Length} chars",
            _deploymentName,
            normalizedText.Length);

        return embedding;
    }

    private static string TruncateToTokenLimit(string text)
    {
        if (text.Length <= MaxCharacters)
            return text;

        // Truncate at last whitespace to avoid cutting mid-word.
        var truncated = text[..MaxCharacters];
        var lastSpace = truncated.LastIndexOf(' ');

        // Fall back to hard cut if no whitespace found (unlikely for resume text).
        return lastSpace > 0 ? truncated[..lastSpace] : truncated;
    }
}
