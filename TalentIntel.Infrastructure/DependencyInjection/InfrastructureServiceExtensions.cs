using Azure;
using Azure.AI.OpenAI;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TalentIntel.Application.Interfaces;
using TalentIntel.Infrastructure.Blob;
using TalentIntel.Infrastructure.Cosmos;
using TalentIntel.Infrastructure.OpenAI;
using TalentIntel.Infrastructure.Parsing;
using TalentIntel.Infrastructure.Queue;

namespace TalentIntel.Infrastructure.DependencyInjection;

/// <summary>
/// Registers all Infrastructure layer services with the DI container.
/// Called from Program.cs in both the API and Functions projects.
/// All clients registered as Singleton; all service implementations as Scoped.
/// </summary>
public static class InfrastructureServiceExtensions
{
    /// <summary>
    /// Registers infrastructure services and Azure clients.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Use a temporary logger to trace registration steps without DI resolution.
        using var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConfiguration(configuration.GetSection("Logging")));
        var logger = loggerFactory.CreateLogger("InfrastructureServiceExtensions");

        logger.LogInformation("Registering infrastructure services");

        // Cosmos client uses a connection string until Managed Identity is enabled.
        var cosmosConnectionString = configuration["Azure:CosmosDb:ConnectionString"];
        if (string.IsNullOrWhiteSpace(cosmosConnectionString))
        {
            throw new InvalidOperationException("Azure:CosmosDb:ConnectionString is not configured.");
        }

        var cosmosSerializerOptions = new CosmosSerializationOptions
        {
            PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            // CamelCase converts "Id" → "id", "FilePath" → "filePath" automatically
        };

        services.AddSingleton(_ => new CosmosClient(cosmosConnectionString,
    new CosmosClientOptions
    {
        SerializerOptions = cosmosSerializerOptions
    }));

        var blobConnectionString = configuration["Azure:BlobStorage:ConnectionString"];
        if (string.IsNullOrWhiteSpace(blobConnectionString))
        {
            throw new InvalidOperationException("Azure:BlobStorage:ConnectionString is not configured.");
        }

        services.AddSingleton(new BlobServiceClient(blobConnectionString));

        var queueConnectionString = configuration["Azure:Queue:ConnectionString"];
        var queueName = configuration["Azure:Queue:QueueName"];
        if (string.IsNullOrWhiteSpace(queueConnectionString) || string.IsNullOrWhiteSpace(queueName))
        {
            throw new InvalidOperationException("Azure:Queue:ConnectionString or Azure:Queue:QueueName is not configured.");
        }

        var serviceBusClient = new ServiceBusClient(queueConnectionString);
        services.AddSingleton(serviceBusClient);
        services.AddSingleton(serviceBusClient.CreateSender(queueName));

        var openAiEndpoint = configuration["Azure:OpenAI:Endpoint"];
        var openAiApiKey = configuration["Azure:OpenAI:ApiKey"];
        if (string.IsNullOrWhiteSpace(openAiEndpoint) || string.IsNullOrWhiteSpace(openAiApiKey))
        {
            throw new InvalidOperationException("Azure:OpenAI:Endpoint or Azure:OpenAI:ApiKey is not configured.");
        }

        services.AddSingleton(
            new AzureOpenAIClient(new Uri(openAiEndpoint), new AzureKeyCredential(openAiApiKey)));

        services.AddScoped<IDocumentRepository, CosmosDocumentRepository>();
        services.AddScoped<IBlobStorageService, BlobStorageService>();
        services.AddScoped<IQueueService, QueueService>();
        services.AddScoped<IEmbeddingService, EmbeddingService>();
        services.AddScoped<IDocumentParserService, DocumentParserService>();

        logger.LogInformation("Infrastructure services registered");

        return services;
    }
}
