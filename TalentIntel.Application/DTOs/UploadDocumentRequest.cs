using Microsoft.AspNetCore.Http;
using TalentIntel.Domain.Enums;

namespace TalentIntel.Application.DTOs;

/// <summary>Request payload for the document upload endpoint.</summary>
public class UploadDocumentRequest
{
    public IFormFile File { get; set; }
    public DocumentType Type { get; set; }

    // Required when Type == Candidate
    public string? CandidateName { get; set; }
    public int? ExperienceYears { get; set; }

    // Required when Type == Job
    public string? JobTitle { get; set; }
}
