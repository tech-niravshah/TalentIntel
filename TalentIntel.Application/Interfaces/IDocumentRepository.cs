using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TalentIntel.Domain.Entities;

namespace TalentIntel.Application.Interfaces;

/// <summary>
/// Data access contract for Cosmos DB document operations.
/// </summary>
public interface IDocumentRepository
{
    /// <summary>Upserts a candidate document in Cosmos DB.</summary>
    Task UpsertCandidateAsync(
        Candidate candidate,
        CancellationToken cancellationToken = default);

    /// <summary>Upserts a job description document in Cosmos DB.</summary>
    Task UpsertJobAsync(
        JobDescription job,
        CancellationToken cancellationToken = default);

    /// <summary>Retrieves a job description by its Cosmos document id.</summary>
    Task<JobDescription?> GetJobAsync(
        string jobId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all candidates whose skills overlap with the required skills
    /// list by at least the given threshold percentage.
    /// Filtering is done in-memory after fetching type="candidate" documents
    /// because Cosmos DB does not support threshold-based array intersection
    /// natively without the vector index preview feature.
    /// </summary>
    Task<IEnumerable<Candidate>> GetCandidatesBySkillsAsync(
        IEnumerable<string> requiredSkills,
        double matchThreshold,
        CancellationToken cancellationToken = default);
}
