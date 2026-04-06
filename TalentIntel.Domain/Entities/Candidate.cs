using System.Text.Json.Serialization;

namespace TalentIntel.Domain.Entities;

/// <summary>
/// Represents a job candidate profile stored in Cosmos DB.
/// Contains parsed resume data including extracted skills and AI-generated embedding.
/// </summary>
public class Candidate
{
    // Cosmos DB document id — format: "candidate-{guid}"
    public string Id { get; set; }

    // Discriminator field — always "candidate". Used as partition key in Cosmos.
    public string Type { get; set; } = "candidate";

    public string Name { get; set; }

    // Normalised lowercase skills extracted from resume text
    public List<string> Skills { get; set; } = new();

    public int ExperienceYears { get; set; }

    // Azure OpenAI text-embedding-ada-002 vector (1536 dimensions)
    public List<float> Embedding { get; set; } = new();

    public DateTime UpdatedAt { get; set; }

    // Tracks which embedding model version generated this vector.
    // Increment when the embedding model changes so stale docs can be re-embedded.
    public string EmbeddingVersion { get; set; } = "v1";

    // Path to the original resume file in Azure Blob Storage
    public string FilePath { get; set; }
}
