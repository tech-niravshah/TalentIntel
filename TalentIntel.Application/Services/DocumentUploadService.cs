using Microsoft.Extensions.Logging;
using System.Text.Json;
using TalentIntel.Application.DTOs;
using TalentIntel.Application.Interfaces;
using TalentIntel.Domain.Enums;

namespace TalentIntel.Application.Services;

/// <summary>
/// Orchestrates the document upload workflow:
/// upload file to Blob Storage → publish queue message for async processing.
/// </summary>
public class DocumentUploadService
{
    // Constructor-injected dependencies
    private readonly IBlobStorageService _blobStorage;
    private readonly IQueueService _queue;
    private readonly ILogger<DocumentUploadService> _logger;

    public DocumentUploadService(
        IBlobStorageService blobStorage,
        IQueueService queue,
        ILogger<DocumentUploadService> logger)
    {
        _blobStorage = blobStorage;
        _queue = queue;
        _logger = logger;
    }

    /// <summary>
    /// Uploads the file to blob storage, creates a queue message, and returns
    /// the new document id. Processing (parsing + embedding) happens asynchronously
    /// via the Azure Function triggered by the queue message.
    /// </summary>
    public async Task<string> UploadAsync(
        UploadDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.File is null)
        {
            throw new ArgumentException("File is required.", nameof(request));
        }

        var documentId = request.Type == DocumentType.Candidate
            ? $"candidate-{Guid.NewGuid()}"
            : $"job-{Guid.NewGuid()}";

        var containerName = request.Type == DocumentType.Candidate ? "candidates" : "jobs";

        await using var fileStream = request.File.OpenReadStream();
        var blobPath = await _blobStorage.UploadAsync(
            fileStream,
            request.File.FileName,
            containerName,
            cancellationToken);

        var message = new QueueMessage
        {
            DocumentId = documentId,
            Type = request.Type,
            BlobPath = blobPath,
            CandidateName = request.CandidateName,
            ExperienceYears = request.ExperienceYears,
            JobTitle = request.JobTitle
        };

        var payload = JsonSerializer.Serialize(message);
        await _queue.PublishAsync(payload, cancellationToken);

        _logger.LogInformation("Uploaded document {DocumentId} to {BlobPath}", documentId, blobPath);

        return documentId;
    }
}
