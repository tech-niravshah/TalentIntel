using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using TalentIntel.Application.Interfaces;
using TalentIntel.Domain.Entities;

namespace TalentIntel.Infrastructure.Cosmos;

/// <summary>
/// Cosmos DB implementation of IDocumentRepository.
/// Uses TWO containers: "Candidates" (partition key /id) and "Jobs" (partition key /id).
/// Reads database name and container names from IConfiguration.
///
/// NOTE on skill filtering: GetCandidatesBySkillsAsync fetches all documents
/// from the Candidates container and filters in-memory.
/// Query: SELECT * FROM c (no WHERE clause — full scan)
/// Then filter: candidates where the count of skills present in the candidate's
/// skill list that also appear in the requested skill set >= threshold parameter.
/// At 50K candidates this is acceptable; at 500K+ add a Cosmos vector index.
/// </summary>
public class CosmosDocumentRepository : IDocumentRepository
{
    private readonly CosmosClient _cosmosClient;
    private readonly string _databaseName;
    private readonly string _candidatesContainerName;
    private readonly string _jobsContainerName;
    private readonly ILogger<CosmosDocumentRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosDocumentRepository"/> class.
    /// </summary>
    public CosmosDocumentRepository(
        CosmosClient cosmosClient,
        IConfiguration configuration,
        ILogger<CosmosDocumentRepository> logger)
    {
        _cosmosClient = cosmosClient;
        _logger = logger;
        _databaseName = configuration["Azure:CosmosDb:DatabaseName"] ?? string.Empty;
        _candidatesContainerName = configuration["Azure:CosmosDb:CandidatesContainer"] ?? string.Empty;
        _jobsContainerName = configuration["Azure:CosmosDb:JobsContainer"] ?? string.Empty;

        // Ensure container names are configured to avoid accidental default usage.
        if (string.IsNullOrWhiteSpace(_databaseName)
            || string.IsNullOrWhiteSpace(_candidatesContainerName)
            || string.IsNullOrWhiteSpace(_jobsContainerName))
        {
            throw new InvalidOperationException("Cosmos DB configuration is incomplete.");
        }
    }

    /// <summary>
    /// Upserts a candidate document in the Candidates container.
    /// </summary>
    public async Task UpsertCandidateAsync(Candidate candidate, CancellationToken cancellationToken = default)
    {
        if (candidate is null)
        {
            throw new ArgumentNullException(nameof(candidate));
        }

        _logger.LogInformation("Upserting candidate {CandidateId}", candidate.Id);

        // Normalize skills to lowercase at storage time for consistent matching.
        candidate.Skills = candidate.Skills
            .Select(skill => skill.Trim().ToLowerInvariant())
            .Where(skill => !string.IsNullOrWhiteSpace(skill))
            .Distinct()
            .ToList();

        await CandidatesContainer.UpsertItemAsync(
            candidate,
            new PartitionKey(candidate.Id),
            cancellationToken: cancellationToken);

        _logger.LogInformation("Upserted candidate {CandidateId}", candidate.Id);
    }

    /// <summary>
    /// Upserts a job description document in the Jobs container.
    /// </summary>
    public async Task UpsertJobAsync(JobDescription job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        _logger.LogInformation("Upserting job {JobId}", job.Id);

        // Normalize skills to lowercase at storage time for consistent matching.
        job.Skills = job.Skills
            .Select(skill => skill.Trim().ToLowerInvariant())
            .Where(skill => !string.IsNullOrWhiteSpace(skill))
            .Distinct()
            .ToList();

        await JobsContainer.UpsertItemAsync(
            job,
            new PartitionKey(job.Id),
            cancellationToken: cancellationToken);

        _logger.LogInformation("Upserted job {JobId}", job.Id);
    }

    /// <summary>
    /// Retrieves a job description by its Cosmos document id.
    /// </summary>
    public async Task<JobDescription?> GetJobAsync(string jobId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            throw new ArgumentException("Job id is required.", nameof(jobId));
        }

        _logger.LogInformation("Fetching job {JobId}", jobId);

        try
        {
            var response = await JobsContainer.ReadItemAsync<JobDescription>(
                jobId,
                new PartitionKey(jobId),
                cancellationToken: cancellationToken);

            _logger.LogInformation("Fetched job {JobId}", jobId);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogInformation("Job {JobId} was not found", jobId);
            return null;
        }
    }

    /// <summary>
    /// Returns candidates filtered by skills overlap threshold.
    /// </summary>
    public async Task<IEnumerable<Candidate>> GetCandidatesBySkillsAsync(
        IEnumerable<string> requiredSkills,
        double matchThreshold,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Scanning candidates for skill match threshold {Threshold}", matchThreshold);

        // Normalize requested skills to lowercase for comparison safety.
        var requiredSkillSet = requiredSkills
            .Select(skill => skill.Trim().ToLowerInvariant())
            .Where(skill => !string.IsNullOrWhiteSpace(skill))
            .ToHashSet();

        var results = new List<Candidate>();
        var queryDefinition = new QueryDefinition("SELECT * FROM c");
        var iterator = CandidatesContainer.GetItemQueryIterator<Candidate>(queryDefinition);

        while (iterator.HasMoreResults)
        {
            // Full container scan is required to support in-memory skill filtering.
            var response = await iterator.ReadNextAsync(cancellationToken);

            foreach (var candidate in response)
            {
                var candidateSkills = candidate.Skills
                    .Select(skill => skill.Trim().ToLowerInvariant())
                    .Where(skill => !string.IsNullOrWhiteSpace(skill))
                    .ToList();

                var matchedCount = candidateSkills.Count(skill => requiredSkillSet.Contains(skill));
                var requiredCount = requiredSkillSet.Count;

                // Compute ratio and compare to threshold percentage.
                var meetsThreshold = requiredCount == 0
                    || (double)matchedCount / requiredCount >= matchThreshold;

                if (meetsThreshold)
                {
                    results.Add(candidate);
                }
            }
        }

        _logger.LogInformation("Skill scan completed with {Count} candidates", results.Count);
        return results;
    }

    private Container CandidatesContainer => _cosmosClient.GetContainer(_databaseName, _candidatesContainerName);

    private Container JobsContainer => _cosmosClient.GetContainer(_databaseName, _jobsContainerName);
}
