namespace AddiScan.Core.Grading;

/// <summary>
/// Risk bands for food additives, based on the final score calculated from the grading criteria.
/// </summary>
public enum RiskBand
{
    Safe,       // No significant concern at typical dietary exposure
    LowRisk,    // Minor concerns. Generally safe at normal intake
    Moderate,   // Some evidence of harm. Caution advised for sensitive groups or high consumers
    HighRisk,   // Significant evidence. Consider avoiding, especially for children
    Avoid,      // Strong evidence of serious harm; banned in multiple jurisdictions
}

/// <summary>
/// Fixed point values from FOOD_ADDITIVE_SAFETY_GRADING.md. Raw points sum to at most <see cref="MaxRaw"/>;
/// final score is clamp(raw, 0, MaxRaw) / MaxRaw * 5, rounded to one decimal place.
/// </summary>
public static class GradingCriteria
{
    public const int CarcinogenicityMax = 4;
    public const int BanStatusMax = 3;
    public const int AllergicReactionsMax = 2;
    public const int CumulativeRiskMax = 2;
    public const int OriginMax = 1;
    public const int ChildrenAndVulnerableMax = 2;
    public const int FunctionalNecessityMin = -1;
    public const int FunctionalNecessityMax = 1;

    public const int MaxRaw = CarcinogenicityMax + BanStatusMax + AllergicReactionsMax
        + CumulativeRiskMax + OriginMax + ChildrenAndVulnerableMax;

    public static RiskBand BandForScore(decimal finalScore) => finalScore switch
    {
        <= 1.0m => RiskBand.Safe,
        <= 2.0m => RiskBand.LowRisk,
        <= 3.0m => RiskBand.Moderate,
        <= 4.0m => RiskBand.HighRisk,
        _ => RiskBand.Avoid,
    };
}
