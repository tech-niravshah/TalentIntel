using System.Threading;
using System.Threading.Tasks;

namespace TalentIntel.Application.Interfaces;

/// <summary>
/// Publishes messages to Azure Storage Queue for async document processing.
/// </summary>
public interface IQueueService
{
    /// <summary>
    /// Serialises and enqueues a message for the document processing function.
    /// </summary>
    Task PublishAsync(string message, CancellationToken cancellationToken = default);
}
