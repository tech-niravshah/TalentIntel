using Microsoft.AspNetCore.Mvc;
using TalentIntel.API.Models;
using TalentIntel.Application.Services;
using TalentIntel.Domain.Enums;

namespace TalentIntel.API.Controllers;

/// <summary>
/// Handles document upload requests (resumes and job descriptions).
/// Delegates to DocumentUploadService — no business logic in the controller.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DocumentsController(DocumentUploadService uploadService, ILogger<DocumentsController> logger) : ControllerBase
{
    private readonly DocumentUploadService _uploadService = uploadService;
    private readonly ILogger<DocumentsController> _logger = logger;

    /// <summary>
    /// Accepts a resume or job description file and enqueues it for async processing.
    /// Returns 202 immediately — embedding generation happens in the background via the Function worker.
    /// </summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(object), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(
        [FromForm] UploadDocumentRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Uploading document of type {DocumentType} with file {FileName}",
            request.DocumentType,
            request.File?.FileName);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (request.File is null || request.File.Length == 0)
        {
            return BadRequest("File is required.");
        }

        // Normalize document type for safe comparison.
        var normalizedType = request.DocumentType?.Trim().ToLowerInvariant();
        DocumentType documentType;

        if (normalizedType == "candidate")
        {
            documentType = DocumentType.Candidate;
        }
        else if (normalizedType == "job")
        {
            documentType = DocumentType.Job;
        }
        else
        {
            return BadRequest("DocumentType must be 'candidate' or 'job'.");
        }

        // Enforce required fields based on the requested document type.
        if (documentType == DocumentType.Candidate && string.IsNullOrWhiteSpace(request.CandidateName))
        {
            return BadRequest("CandidateName is required for candidate documents.");
        }

        if (documentType == DocumentType.Job && string.IsNullOrWhiteSpace(request.JobTitle))
        {
            return BadRequest("JobTitle is required for job documents.");
        }

        var uploadRequest = new TalentIntel.Application.DTOs.UploadDocumentRequest
        {
            File = request.File,
            Type = documentType,
            CandidateName = request.CandidateName,
            JobTitle = request.JobTitle
        };

        var documentId = await _uploadService.UploadAsync(uploadRequest, cancellationToken);

        _logger.LogInformation("Uploaded document {DocumentId} of type {DocumentType}", documentId, documentType);

        return Accepted(new { documentId });
    }
}
