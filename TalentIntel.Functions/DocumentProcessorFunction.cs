using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TalentIntel.Application.DTOs;
using TalentIntel.Application.Interfaces;
using TalentIntel.Domain.Entities;
using TalentIntel.Domain.Enums;


namespace TalentIntel.Functions.Functions;

/// <summary>
/// Azure Function triggered by Azure Service Bus Queue messages.
/// Processes uploaded documents asynchronously:
///   1. Deserialize QueueMessage from raw JSON string
///   2. Download raw file stream from Blob Storage using message.BlobPath
///   3. Determine file type from BlobPath extension (.pdf or .docx)
///   4. Extract text from stream via IDocumentParserService
///   5. Extract skills via keyword matching
///   6. Generate embedding via Azure OpenAI
///   7. Persist structured document to Cosmos DB
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DocumentProcessorFunction"/> class.
/// </remarks>
public class DocumentProcessorFunction(
    IBlobStorageService blobStorage,
    IDocumentParserService parser,
    IEmbeddingService embeddingService,
    IDocumentRepository repository,
    ILogger<DocumentProcessorFunction> logger)
{
    private readonly IBlobStorageService _blobStorage = blobStorage;
    private readonly IDocumentParserService _parser = parser;
    private readonly IEmbeddingService _embeddingService = embeddingService;
    private readonly IDocumentRepository _repository = repository;
    private readonly ILogger<DocumentProcessorFunction> _logger = logger;

    /// <summary>
    /// Service Bus trigger — fires when a message arrives in the queue.
    /// Connection = "ServiceBusConnection" is a flat key resolved from config.
    /// Message body is a UTF-8 JSON string — no Base64 decoding needed.
    /// </summary>
    [Function("DocumentProcessor")]
    public async Task Run(
        [ServiceBusTrigger("talentintel-docprocessing-queue", Connection = "ServiceBusConnection")] string rawMessage,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received Service Bus message");

        // Case-insensitive JSON helps with sender casing variations.
        var message = JsonSerializer.Deserialize<QueueMessage>(
            rawMessage,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (message is null)
        {
            throw new InvalidOperationException("Queue message payload was empty.");
        }

        if (string.IsNullOrWhiteSpace(message.BlobPath))
        {
            throw new InvalidOperationException("BlobPath is required in the queue message.");
        }

        _logger.LogInformation(
            "Processing document {DocumentId} of type {Type}",
            message.DocumentId,
            message.Type);

        // Use the local path to safely extract the filename from the blob URI.
        var fileName = Path.GetFileName(new Uri(message.BlobPath).LocalPath);

        _logger.LogInformation("Downloading blob {BlobPath}", message.BlobPath);
        await using var stream = await _blobStorage.DownloadAsync(message.BlobPath, cancellationToken);
        _logger.LogInformation("Downloaded blob {BlobPath}", message.BlobPath);

        _logger.LogInformation("Extracting text for {DocumentId}", message.DocumentId);
        var rawText = await _parser.ExtractTextAsync(stream, fileName, cancellationToken);
        _logger.LogInformation("Extracted text for {DocumentId}", message.DocumentId);

        _logger.LogInformation("Extracting skills for {DocumentId}", message.DocumentId);
        // Normalize skills to lowercase at storage time.
        var skills = _parser.ExtractSkills(rawText)
            .Select(skill => skill.ToLowerInvariant())
            .Distinct()
            .ToList();
        _logger.LogInformation("Extracted skills for {DocumentId}", message.DocumentId);

        _logger.LogInformation("Generating embedding for {DocumentId}", message.DocumentId);
        var embedding = await _embeddingService.GenerateEmbeddingAsync(rawText, cancellationToken);
        _logger.LogInformation("Generated embedding for {DocumentId}", message.DocumentId);

        if (message.Type == DocumentType.Candidate)
        {
            if (string.IsNullOrWhiteSpace(message.CandidateName))
            {
                throw new InvalidOperationException("CandidateName is required for candidate documents.");
            }

            var candidate = new Candidate
            {
                Id = message.DocumentId,
                Name = message.CandidateName,
                ExperienceYears = message.ExperienceYears.GetValueOrDefault(),
                Skills = skills,
                FilePath = message.BlobPath,
                Embedding = embedding,
                UpdatedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Persisting candidate document {DocumentId}", message.DocumentId);
            await _repository.UpsertCandidateAsync(candidate, cancellationToken);
            _logger.LogInformation("Persisted candidate document {DocumentId}", message.DocumentId);
        }
        else if (message.Type == DocumentType.Job)
        {
            if (string.IsNullOrWhiteSpace(message.JobTitle))
            {
                throw new InvalidOperationException("JobTitle is required for job documents.");
            }

            var job = new JobDescription
            {
                Id = message.DocumentId,
                Title = message.JobTitle,
                Skills = skills,
                FilePath = message.BlobPath,
                Embedding = embedding,
                UpdatedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Persisting job document {DocumentId}", message.DocumentId);
            await _repository.UpsertJobAsync(job, cancellationToken);
            _logger.LogInformation("Persisted job document {DocumentId}", message.DocumentId);
        }
        else
        {
            throw new InvalidOperationException($"Unsupported document type {message.Type}.");
        }

        _logger.LogInformation("Successfully processed document {DocumentId}", message.DocumentId);
    }
}