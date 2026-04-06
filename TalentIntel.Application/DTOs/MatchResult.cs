using System.Collections.Generic;

namespace TalentIntel.Application.DTOs;

/// <summary>Represents a single ranked candidate result returned by the matching API.</summary>
public class MatchResult
{
    public string CandidateId { get; set; }
    public string Name { get; set; }
    public List<string> Skills { get; set; }

    // Cosine similarity score between candidate and job embeddings (0.0 – 1.0)
    public double SemanticScore { get; set; }

    // Proportion of job's required skills present on the candidate profile
    public double SkillScore { get; set; }

    // Weighted composite: (Semantic * 0.6) + (Skill * 0.25) + (Experience * 0.15)
    public double CompositeScore { get; set; }

    public string Interpretation { get; set; }
}
