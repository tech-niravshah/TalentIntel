using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace TalentIntel.API.Models;

/// <summary>
/// Multipart form data model for document upload.
/// Used by DocumentsController.Upload.
/// </summary>
public class UploadDocumentRequest
{
    /// <summary>
    /// Gets or sets the resume or job description file.
    /// </summary>
    [Required]
    public IFormFile File { get; set; }

    /// <summary>
    /// Gets or sets the document type string ("candidate" or "job").
    /// </summary>
    [Required]
    public string DocumentType { get; set; }

    /// <summary>
    /// Gets or sets the candidate name when uploading a resume.
    /// </summary>
    public string? CandidateName { get; set; }

    /// <summary>
    /// Gets or sets the job title when uploading a job description.
    /// </summary>
    public string? JobTitle { get; set; }
}
