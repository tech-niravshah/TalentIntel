using System;
using System.Collections.Generic;
using System.Linq;
using TalentIntel.Application.Helpers;
using Xunit;

namespace TalentIntel.Tests;

/// <summary>
/// Tests for VectorMath.CosineSimilarity covering edge cases and known values.
/// VectorMath is a static class — no mocking needed.
/// </summary>
public class VectorMathTests
{
    /// <summary>
    /// Test: identical vectors [1,0,0] and [1,0,0] → result == 1.0.
    /// </summary>
    [Fact]
    public void CosineSimilarity_IdenticalVectors_ReturnsOne()
    {
        var vectorA = new float[] { 1f, 0f, 0f };
        var vectorB = new float[] { 1f, 0f, 0f };

        var result = VectorMath.CosineSimilarity(vectorA.ToList(), vectorB.ToList());

        Assert.Equal(1.0, result, 4);
    }

    /// <summary>
    /// Test: orthogonal vectors [1,0] and [0,1] → result == 0.0.
    /// </summary>
    [Fact]
    public void CosineSimilarity_OrthogonalVectors_ReturnsZero()
    {
        var vectorA = new float[] { 1f, 0f };
        var vectorB = new float[] { 0f, 1f };

        var result = VectorMath.CosineSimilarity(vectorA.ToList(), vectorB.ToList());

        Assert.Equal(0.0, result, 4);
    }

    /// <summary>
    /// Test: zero vector [0,0,0] → result == 0.0, no DivideByZeroException thrown.
    /// </summary>
    [Fact]
    public void CosineSimilarity_ZeroVector_ReturnsZero()
    {
        var vectorA = new float[] { 0f, 0f, 0f };
        var vectorB = new float[] { 0f, 0f, 0f };

        var result = VectorMath.CosineSimilarity(vectorA.ToList(), vectorB.ToList());

        Assert.Equal(0.0, result, 4);
    }

    /// <summary>
    /// Test: opposite vectors [1,0] and [-1,0] → result == -1.0.
    /// </summary>
    [Fact]
    public void CosineSimilarity_OppositeVectors_ReturnsNegativeOne()
    {
        var vectorA = new float[] { 1f, 0f };
        var vectorB = new float[] { -1f, 0f };

        var result = VectorMath.CosineSimilarity(vectorA.ToList(), vectorB.ToList());

        Assert.Equal(-1.0, result, 4);
    }

    /// <summary>
    /// Test: mismatched lengths → result == 0.0, no exception thrown.
    /// </summary>
    [Fact]
    public void CosineSimilarity_MismatchedLengths_ReturnsZero()
    {
        var vectorA = new float[] { 1f, 0f };
        var vectorB = new float[] { 1f, 0f, 0f };

        var result = VectorMath.CosineSimilarity(vectorA.ToList(), vectorB.ToList());

        Assert.Equal(0.0, result, 4);
    }

    /// <summary>
    /// Test: known values [1,2,3] and [4,5,6] → assert result is within 0.0001 tolerance.
    /// expected ≈ 0.9746 (compute manually: dot=32, magA=√14, magB=√77).
    /// </summary>
    [Fact]
    public void CosineSimilarity_KnownValues_ReturnsExpectedResult()
    {
        var vectorA = new float[] { 1f, 2f, 3f };
        var vectorB = new float[] { 4f, 5f, 6f };

        var result = VectorMath.CosineSimilarity(vectorA.ToList(), vectorB.ToList());

        Assert.True(Math.Abs(result - 0.9746) < 0.0001);
    }
}
