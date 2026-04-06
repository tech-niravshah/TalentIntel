using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TalentIntel.Application.Interfaces;

/// <summary>
/// Handles uploading raw documents to Azure Blob Storage.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Uploads a file stream to blob storage and returns the blob path.
    /// </summary>
    Task<string> UploadAsync(
        Stream fileStream,
        string fileName,
        string containerName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a blob as a stream from the specified path.
    /// </summary>
    /// <param name="blobPath">The path to the blob to download.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A stream containing the blob data.</returns>
    Task<Stream> DownloadAsync(string blobPath, CancellationToken cancellationToken = default);
}
