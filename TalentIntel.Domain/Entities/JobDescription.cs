namespace TalentIntel.Domain.Entities;

/// <summary>
/// Represents a job posting stored in Cosmos DB.
/// Contains parsed JD data including required skills and AI-generated embedding.
/// </summary>
public class JobDescription
{
    // Cosmos DB document id — format: "job-{guid}"
    public string Id { get; set; }

    // Discriminator field — always "job". Used as partition key in Cosmos.
    public string Type { get; set; } = "job";

    public string Title { get; set; }

    // Required skills extracted from the job description — lowercase normalised
    public List<string> Skills { get; set; } = new();

    // Azure OpenAI text-embedding-ada-002 vector (1536 dimensions)
    public List<float> Embedding { get; set; } = new();

    public DateTime UpdatedAt { get; set; }

    public string EmbeddingVersion { get; set; } = "v1";

    // Path to the original JD file in Azure Blob Storage
    public string FilePath { get; set; }
}
