using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;
using TalentIntel.Application.DTOs;
using TalentIntel.Application.Helpers;
using TalentIntel.Application.Interfaces;

namespace TalentIntel.Application.Services;

/// <summary>
/// Core ranking service. Implements two-stage retrieval:
/// Stage 1 — skills-based filter from Cosmos DB (cheap SQL query)
/// Stage 2 — cosine similarity on filtered candidate embeddings (precise)
/// Returns the top 10 candidates by composite score.
/// </summary>
public class MatchingService
{
    private readonly IDocumentRepository _repository;
    private readonly TelemetryClient _telemetryClient;
    private readonly ILogger<MatchingService> _logger;

    public MatchingService(
        IDocumentRepository repository,
        TelemetryClient telemetryClient,
        ILogger<MatchingService> logger)
    {
        _repository = repository;
        _telemetryClient = telemetryClient;
        _logger = logger;
    }

    /// <summary>
    /// Returns the top 10 best-matched candidates for a given job id.
    /// Throws NotFoundException if the job does not exist in Cosmos DB.
    /// Skills are normalised to lowercase during comparison as a defensive measure.
    /// </summary>
    public async Task<List<MatchResult>> GetTopCandidatesAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            throw new ArgumentException("Job id is required.", nameof(jobId));
        }

        var job = await _repository.GetJobAsync(jobId, cancellationToken);
        if (job is null)
        {
            throw new KeyNotFoundException($"Job '{jobId}' not found.");
        }

        var requiredSkills = job.Skills ?? new List<string>();
        var requiredSkillSet = requiredSkills
            .Select(skill => skill.Trim().ToLowerInvariant())
            .ToHashSet();

        var candidates = await _repository.GetCandidatesBySkillsAsync(requiredSkills, 0.6, cancellationToken);
        var results = new List<MatchResult>();
        var evaluatedCount = 0;

        foreach (var candidate in candidates)
        {
            evaluatedCount++;
            var candidateSkills = candidate.Skills ?? new List<string>();
            var matchedSkills = candidateSkills
                .Select(skill => skill.Trim().ToLowerInvariant())
                .Count(skill => requiredSkillSet.Contains(skill));
            var skillScore = requiredSkills.Count > 0 ? (double)matchedSkills / requiredSkills.Count : 0.0;
            var semanticScore = VectorMath.CosineSimilarity(candidate.Embedding, job.Embedding);
            var experienceScore = Math.Min(candidate.ExperienceYears, 10) / 10.0;
            var compositeScore = (semanticScore * 0.7) + (skillScore * 0.3);

            results.Add(new MatchResult
            {
                CandidateId = candidate.Id,
                Name = candidate.Name,
                Skills = candidateSkills,
                SemanticScore = semanticScore,
                SkillScore = skillScore,
                CompositeScore = compositeScore,
                Interpretation = InterpretScore(compositeScore)
            });
        }

        var topResults = results
            .OrderByDescending(result => result.CompositeScore)
            .Take(10)
            .ToList();

        _telemetryClient.TrackEvent(
            "MatchingService.GetTopCandidates",
            new Dictionary<string, string> { ["JobId"] = jobId },
            new Dictionary<string, double>
            {
                ["EvaluatedCount"] = evaluatedCount,
                ["TopScore"] = topResults.FirstOrDefault()?.CompositeScore ?? 0
            });

        _logger.LogInformation(
            "Matched {Count} candidates for job {JobId}. Top score: {TopScore}",
            evaluatedCount,
            jobId,
            topResults.FirstOrDefault()?.CompositeScore ?? 0);

        return topResults;
    }

    public static string InterpretScore(double score)
    {
        return score switch
        {
            >= 0.90 => "Very High Similarity (almost identical meaning)",
            >= 0.75 => "High Similarity (related topic)",
            >= 0.50 => "Moderate Similarity (somewhat related)",
            >= 0.25 => "Low Similarity (loosely related)",
            _ => "Not Similar (different topics)"
        };
    }
}
