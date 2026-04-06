using System;
using System.Collections.Generic;
using System.Linq;

namespace TalentIntel.Application.Helpers;

/// <summary>
/// Mathematical helpers for vector operations used in semantic similarity scoring.
/// </summary>
public static class VectorMath
{
    /// <summary>
    /// Computes cosine similarity between two float vectors.
    /// Returns 0.0 if either vector is null, empty, or has zero magnitude.
    /// Result range: -1.0 (opposite) to 1.0 (identical direction).
    /// </summary>
    public static double CosineSimilarity(List<float> a, List<float> b)
    {
        if (a is null || b is null || a.Count == 0 || b.Count == 0 || a.Count != b.Count)
        {
            return 0.0;
        }

        var dotProduct = 0.0;
        var magnitudeA = 0.0;
        var magnitudeB = 0.0;

        for (var i = 0; i < a.Count; i++)
        {
            dotProduct += a[i] * b[i];
            magnitudeA += a[i] * a[i];
            magnitudeB += b[i] * b[i];
        }

        if (dotProduct == 0.0)
        {
            return 0.0;
        }

        var denominator = Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB);
        if (denominator == 0.0)
        {
            return 0.0;
        }

        return dotProduct / denominator;
    }
}
