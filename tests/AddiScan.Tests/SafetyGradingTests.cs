using AddiScan.Core.Entities;
using AddiScan.Core.Grading;

namespace AddiScan.Tests;

public class SafetyGradingTests
{
    private static SafetyGrading MakeGraded(int rawPoints, decimal finalScore, RiskBand riskBand, int functionalNecessityScore) => new()
    {
        Graded = true,
        RawPoints = rawPoints,
        FinalScore = finalScore,
        RiskBand = riskBand,
        FunctionalNecessityScore = functionalNecessityScore,
    };

    [Fact]
    public void ComputeEffectiveScore_IncludingFunctionalNecessity_ReturnsStoredValues()
    {
        var grading = MakeGraded(rawPoints: 10, finalScore: 3.6m, riskBand: RiskBand.HighRisk, functionalNecessityScore: -1);

        var effective = grading.ComputeEffectiveScore(includeFunctionalNecessity: true);

        Assert.Equal((10, 3.6m, RiskBand.HighRisk), effective);
    }

    [Fact]
    public void ComputeEffectiveScore_ExcludingFunctionalNecessity_AddsBackTheModifier()
    {
        // Raw points already reflect a -1 functional necessity penalty; dropping it should raise the score.
        var grading = MakeGraded(rawPoints: 10, finalScore: 3.6m, riskBand: RiskBand.HighRisk, functionalNecessityScore: -1);

        var effective = grading.ComputeEffectiveScore(includeFunctionalNecessity: false);

        Assert.Equal(11, effective?.RawPoints);
        Assert.True(effective?.FinalScore > 3.6m);
    }

    [Fact]
    public void ComputeEffectiveScore_ExcludingFunctionalNecessity_ClampsAtMaxRaw()
    {
        // A +1 functional necessity bonus already pushed raw points to the max; removing it should clamp, not overflow.
        var grading = MakeGraded(rawPoints: GradingCriteria.MaxRaw, finalScore: 5.0m, riskBand: RiskBand.Avoid, functionalNecessityScore: 1);

        var effective = grading.ComputeEffectiveScore(includeFunctionalNecessity: false);

        Assert.Equal(GradingCriteria.MaxRaw - 1, effective?.RawPoints);
    }

    [Fact]
    public void ComputeEffectiveScore_ReturnsNullWhenUngraded()
    {
        var grading = new SafetyGrading { Graded = false };

        Assert.Null(grading.ComputeEffectiveScore(includeFunctionalNecessity: false));
    }
}
