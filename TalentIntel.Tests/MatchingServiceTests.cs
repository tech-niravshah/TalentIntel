using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;
using Moq;
using TalentIntel.Application.Interfaces;
using TalentIntel.Application.Services;
using TalentIntel.Domain.Entities;

namespace TalentIntel.Tests;

/// <summary>
/// Unit tests for MatchingService two-stage retrieval ranking.
/// Mocks IDocumentRepository using Moq.
/// Does NOT test infrastructure (Cosmos, OpenAI) — those are mocked.
/// </summary>
public class MatchingServiceTests
{
    private readonly Mock<IDocumentRepository> _repoMock;
    private readonly MatchingService _sut;

    /// <summary>
    /// Initializes the test fixture and shared dependencies.
    /// </summary>
    public MatchingServiceTests()
    {
        _repoMock = new Mock<IDocumentRepository>();
        var telemetryClient = new TelemetryClient(new TelemetryConfiguration());
        var logger = new Mock<ILogger<MatchingService>>();

        _sut = new MatchingService(_repoMock.Object, telemetryClient, logger.Object);
    }

    /// <summary>
    /// Test: job not found.
    /// </summary>
    [Fact]
    public async Task GetTopCandidatesAsync_JobNotFound_ThrowsNotFoundException()
    {
        _repoMock
            .Setup(repo => repo.GetJobAsync("job-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((JobDescription?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.GetTopCandidatesAsync("job-123", CancellationToken.None));
    }

    /// <summary>
    /// Test: no candidates pass skill filter.
    /// </summary>
    [Fact]
    public async Task GetTopCandidatesAsync_NoCandidates_ReturnsEmptyList()
    {
        var job = new JobDescription
        {
            Id = "job-123",
            Title = "Cloud Engineer",
            Skills = new List<string> { "azure", "docker" },
            Embedding = new List<float> { 1f, 0f }
        };

        _repoMock
            .Setup(repo => repo.GetJobAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _repoMock
            .Setup(repo => repo.GetCandidatesBySkillsAsync(job.Skills, 0.6, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Candidate>());

        var results = await _sut.GetTopCandidatesAsync(job.Id, CancellationToken.None);

        Assert.NotNull(results);
        Assert.Empty(results);
    }

    /// <summary>
    /// Test: highest composite score ranked first.
    /// </summary>
    [Fact]
    public async Task GetTopCandidatesAsync_HighestCompositeScore_RanksFirst()
    {
        var job = new JobDescription
        {
            Id = "job-456",
            Title = "Platform Engineer",
            Skills = new List<string> { "kubernetes", "terraform" },
            Embedding = new List<float> { 1f, 0f }
        };

        var candidateA = new Candidate
        {
            Id = "candidate-a",
            Name = "Candidate A",
            Skills = new List<string> { "kubernetes", "terraform" },
            Embedding = new List<float> { 1f, 0f },
            ExperienceYears = 5
        };

        var candidateB = new Candidate
        {
            Id = "candidate-b",
            Name = "Candidate B",
            Skills = new List<string> { "kubernetes" },
            Embedding = new List<float> { 0f, 1f },
            ExperienceYears = 5
        };

        _repoMock
            .Setup(repo => repo.GetJobAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _repoMock
            .Setup(repo => repo.GetCandidatesBySkillsAsync(job.Skills, 0.6, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { candidateA, candidateB });

        var results = await _sut.GetTopCandidatesAsync(job.Id, CancellationToken.None);

        Assert.NotEmpty(results);
        Assert.Equal(candidateA.Id, results[0].CandidateId);
    }

    /// <summary>
    /// Test: result count never exceeds 10.
    /// </summary>
    [Fact]
    public async Task GetTopCandidatesAsync_ResultCount_DoesNotExceedTen()
    {
        var job = new JobDescription
        {
            Id = "job-789",
            Title = "Data Engineer",
            Skills = new List<string> { "sql" },
            Embedding = new List<float> { 1f, 0f }
        };

        var candidates = Enumerable.Range(1, 20)
            .Select(index => new Candidate
            {
                Id = $"candidate-{index}",
                Name = $"Candidate {index}",
                Skills = new List<string> { "sql" },
                Embedding = new List<float> { 1f, 0f },
                ExperienceYears = index
            })
            .ToList();

        _repoMock
            .Setup(repo => repo.GetJobAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _repoMock
            .Setup(repo => repo.GetCandidatesBySkillsAsync(job.Skills, 0.6, It.IsAny<CancellationToken>()))
            .ReturnsAsync(candidates);

        var results = await _sut.GetTopCandidatesAsync(job.Id, CancellationToken.None);

        Assert.True(results.Count <= 10);
    }

    /// <summary>
    /// Test: composite score calculated correctly.
    /// </summary>
    [Fact]
    public async Task GetTopCandidatesAsync_CompositeScoreCalculatedCorrectly()
    {
        var job = new JobDescription
        {
            Id = "job-999",
            Title = "Backend Developer",
            Skills = new List<string> { "c#", "azure", "sql", "docker" },
            Embedding = new List<float> { 1f, 0f }
        };

        var candidate = new Candidate
        {
            Id = "candidate-1",
            Name = "Candidate 1",
            Skills = new List<string> { "c#", "azure", "sql" },
            Embedding = new List<float> { 1f, 0f },
            ExperienceYears = 5
        };

        _repoMock
            .Setup(repo => repo.GetJobAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _repoMock
            .Setup(repo => repo.GetCandidatesBySkillsAsync(job.Skills, 0.6, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { candidate });

        var results = await _sut.GetTopCandidatesAsync(job.Id, CancellationToken.None);

        Assert.Single(results);
        Assert.True(Math.Abs(results[0].CompositeScore - 0.90) < 0.04);
    }
}
