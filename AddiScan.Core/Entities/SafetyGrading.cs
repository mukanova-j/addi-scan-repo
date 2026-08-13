using AddiScan.Core.Grading;

namespace AddiScan.Core.Entities;

/// <summary>Owned by <see cref="Additive"/>. Per-criterion max/range values are fixed constants in <see cref="GradingCriteria"/>, not stored here.</summary>
public class SafetyGrading
{
    public bool Graded { get; set; }
    public decimal? FinalScore { get; set; }
    public int? RawPoints { get; set; }
    public RiskBand? RiskBand { get; set; }

    public int? CarcinogenicityScore { get; set; }
    public string? CarcinogenicityNote { get; set; }

    public int? BanStatusScore { get; set; }
    public string? BanStatusNote { get; set; }

    public int? AllergicReactionsScore { get; set; }
    public string? AllergicReactionsNote { get; set; }

    public int? CumulativeRiskScore { get; set; }
    public string? CumulativeRiskNote { get; set; }

    public int? OriginScore { get; set; }
    public string? OriginNote { get; set; }

    public int? ChildrenAndVulnerableScore { get; set; }
    public string? ChildrenAndVulnerableNote { get; set; }

    public int? FunctionalNecessityScore { get; set; }
    public string? FunctionalNecessityNote { get; set; }

    /// <summary>
    /// Returns the raw points, final score, and risk band to use, optionally dropping the
    /// functional necessity modifier baked into the stored <see cref="RawPoints"/> — used to
    /// honor a user's setting to ignore it. When <paramref name="includeFunctionalNecessity"/>
    /// is true this just returns the stored values unchanged, since those were computed with
    /// the modifier applied. Returns null when this additive has not been graded.
    /// </summary>
    public (int RawPoints, decimal FinalScore, RiskBand RiskBand)? ComputeEffectiveScore(bool includeFunctionalNecessity)
    {
        if (!Graded || RiskBand is not { } riskBand)
        {
            return null;
        }

        if (includeFunctionalNecessity)
        {
            return (RawPoints ?? 0, FinalScore ?? 0m, riskBand);
        }

        var raw = (RawPoints ?? 0) - (FunctionalNecessityScore ?? 0);
        var clamped = Math.Clamp(raw, 0, GradingCriteria.MaxRaw);
        return (clamped, clamped, GradingCriteria.BandForScore(clamped));
    }   
}
