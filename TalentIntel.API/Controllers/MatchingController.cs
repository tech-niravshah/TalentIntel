using Microsoft.AspNetCore.Mvc;
using TalentIntel.Application.DTOs;
using TalentIntel.Application.Services;

namespace TalentIntel.API.Controllers;

/// <summary>
/// Handles candidate ranking requests for a given job opening.
/// Delegates to MatchingService — no business logic in the controller.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MatchingController(MatchingService matchingService, ILogger<MatchingController> logger) : ControllerBase
{
    private readonly MatchingService _matchingService = matchingService;
    private readonly ILogger<MatchingController> _logger = logger;

    /// <summary>
    /// Returns the top 10 candidates ranked by two-stage retrieval:
    /// skill overlap filter first, then cosine similarity on embeddings.
    /// </summary>
    [HttpGet("jobs/{jobId}/top-candidates")]
    [ProducesResponseType(typeof(List<MatchResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTopCandidates(
        string jobId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            return BadRequest("jobId is required.");

        _logger.LogInformation("Fetching top candidates for job {JobId}", jobId);

        try
        {
            var results = await _matchingService.GetTopCandidatesAsync(jobId, cancellationToken);

            _logger.LogInformation("Fetched {Count} candidates for job {JobId}", results.Count, jobId);
            return Ok(results);
        }
        catch (KeyNotFoundException)
        {
            // Matching service throws when the job id cannot be found.
            _logger.LogInformation("Job {JobId} was not found", jobId);
            return NotFound();
        }
    }
}
