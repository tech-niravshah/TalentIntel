using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TalentIntel.Application.Interfaces;

namespace TalentIntel.Infrastructure.Blob;

/// <summary>
/// Azure Blob Storage implementation of IBlobStorageService.
/// Uploads raw resume and JD files to the configured container.
/// Container name is read from configuration key Azure:BlobStorage:ContainerName.
/// Creates the container if it does not exist on first upload.
/// Returns the blob URI as a string.
/// </summary>
public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;
    private readonly ILogger<BlobStorageService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobStorageService"/> class.
    /// </summary>
    public BlobStorageService(
        BlobServiceClient blobServiceClient,
        IConfiguration configuration,
        ILogger<BlobStorageService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _logger = logger;
        _containerName = configuration["Azure:BlobStorage:ContainerName"] ?? string.Empty;
    }

    /// <summary>
    /// Uploads a file stream to blob storage and returns the blob path.
    /// </summary>
    public async Task<string> UploadAsync(
        Stream fileStream,
        string fileName,
        string containerName,
        CancellationToken cancellationToken = default)
    {
        if (fileStream is null)
        {
            throw new ArgumentNullException(nameof(fileStream));
        }

        _logger.LogInformation("Uploading blob {FileName}", fileName);

        // Prefer configured container name, falling back to the provided value if missing.
        var targetContainerName = string.IsNullOrWhiteSpace(_containerName) ? containerName : _containerName;
        var containerClient = _blobServiceClient.GetBlobContainerClient(targetContainerName);

        await containerClient.CreateIfNotExistsAsync(
            PublicAccessType.None,
            cancellationToken: cancellationToken);

        var blobClient = containerClient.GetBlobClient(fileName);

        // Default content type avoids forcing a MIME lookup for generic uploads.
        var headers = new BlobHttpHeaders { ContentType = "application/octet-stream" };

        await blobClient.UploadAsync(
            fileStream,
            new BlobUploadOptions { HttpHeaders = headers },
            cancellationToken);

        _logger.LogInformation("Uploaded blob {FileName} to {BlobUri}", fileName, blobClient.Uri);
        return blobClient.Uri.ToString();
    }

    /// <summary>
    /// Downloads a blob as a stream from the specified path.
    /// </summary>
    /// <param name="blobPath">The path to the blob to download.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A stream containing the blob data.</returns>
    public async Task<Stream> DownloadAsync(string blobPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(blobPath))
        {
            throw new ArgumentException("Blob path is required.", nameof(blobPath));
        }

        _logger.LogInformation("Downloading blob {BlobPath}", blobPath);

        var blobUri = new BlobUriBuilder(new Uri(blobPath));
        var targetContainerName = string.IsNullOrWhiteSpace(_containerName)
            ? blobUri.BlobContainerName
            : _containerName;

        // Use the full blob name to preserve any virtual folder segments.
        var containerClient = _blobServiceClient.GetBlobContainerClient(targetContainerName);
        var blobClient = containerClient.GetBlobClient(blobUri.BlobName);

        var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
        _logger.LogInformation("Downloaded blob {BlobPath}", blobPath);

        return response.Value.Content;
    }
}
