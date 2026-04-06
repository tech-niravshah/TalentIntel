using TalentIntel.Domain.Enums;

namespace TalentIntel.Application.DTOs;

/// <summary>
/// Message payload published to Azure Storage Queue after a file is uploaded.
/// Consumed by the Azure Function worker.
/// </summary>
public class QueueMessage
{
    // Unique document id — same value used as the Cosmos DB document id
    public string DocumentId { get; set; }
    public DocumentType Type { get; set; }

    // Blob path of the uploaded file — used by the Function to download it
    public string BlobPath { get; set; }

    public string? CandidateName { get; set; }
    public int? ExperienceYears { get; set; }
    public string? JobTitle { get; set; }
}
