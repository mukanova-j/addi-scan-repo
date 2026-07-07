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
}
