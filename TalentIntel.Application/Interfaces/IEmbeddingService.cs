using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TalentIntel.Application.Interfaces;

/// <summary>
/// Generates dense vector embeddings via Azure OpenAI.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Calls Azure OpenAI text-embedding-3-small and returns a 1536-d float vector.
    /// </summary>
    Task<List<float>> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default);
}
