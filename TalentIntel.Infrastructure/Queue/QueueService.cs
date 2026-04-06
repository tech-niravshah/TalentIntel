using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using TalentIntel.Application.Interfaces;

namespace TalentIntel.Infrastructure.Queue;

/// <summary>
/// Azure Service Bus implementation of IQueueService.
/// Queue name is read from configuration key Queue__QueueName.
/// </summary>
public class QueueService : IQueueService
{
    private readonly ServiceBusSender _sender;
    private readonly ILogger<QueueService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueueService"/> class.
    /// </summary>
    public QueueService(ServiceBusSender sender, ILogger<QueueService> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    /// <summary>
    /// Serialises and enqueues a message for the document processing function.
    /// </summary>
    public async Task PublishAsync(string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Publishing queue message {Message}", message);

        var serviceBusMessage = new ServiceBusMessage(message);

        await _sender.SendMessageAsync(serviceBusMessage, cancellationToken);

        _logger.LogInformation("Published queue message {Message}", message);
    }
}
