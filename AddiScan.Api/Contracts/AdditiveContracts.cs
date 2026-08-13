using AddiScan.Core.Entities;

namespace AddiScan.Api.Contracts;

/// <summary>
/// Additive summary response containing basic information about an additive, including its grading status and final score.
/// </summary>
/// <param name="Id"></param>
/// <param name="ENumber"></param>
/// <param name="Name"></param>
/// <param name="Graded"></param>
/// <param name="FinalScore"></param>
/// <param name="RiskBand"></param>
public record AdditiveSummaryResponse(
    int Id,
    string? ENumber,
    string Name,
    bool Graded,
    decimal? FinalScore,
    string? RiskBand)
{
    public static AdditiveSummaryResponse FromEntity(Additive additive, bool includeFunctionalNecessity = true)
    {
        var effective = additive.Grading?.ComputeEffectiveScore(includeFunctionalNecessity);
        return new(
            additive.Id,
            additive.ENumber,
            additive.Name,
            additive.Grading?.Graded ?? false,
            effective?.FinalScore,
            effective?.RiskBand.ToString());
    }
}

/// <summary>
/// Criterion score response containing the score and note for a specific grading criterion.
/// </summary>
/// <param name="Score"></param>
/// <param name="Note"></param>
public record CriterionScoreResponse(int? Score, string? Note);

/// <summary>
/// Safety grading response for an additive, including scores and notes for various criteria.
/// </summary>
/// <param name="Graded"></param>
/// <param name="FinalScore"></param>
/// <param name="RawPoints"></param>
/// <param name="RiskBand"></param>
/// <param name="Carcinogenicity"></param>
/// <param name="BanStatus"></param>
/// <param name="AllergicReactions"></param>
/// <param name="CumulativeRisk"></param>
/// <param name="Origin"></param>
/// <param name="ChildrenAndVulnerable"></param>
/// <param name="FunctionalNecessity"></param>
public record SafetyGradingResponse(
    bool Graded,
    decimal? FinalScore,
    int? RawPoints,
    string? RiskBand,
    CriterionScoreResponse Carcinogenicity,
    CriterionScoreResponse BanStatus,
    CriterionScoreResponse AllergicReactions,
    CriterionScoreResponse CumulativeRisk,
    CriterionScoreResponse Origin,
    CriterionScoreResponse ChildrenAndVulnerable,
    CriterionScoreResponse FunctionalNecessity)
{
    public static SafetyGradingResponse FromEntity(SafetyGrading grading, bool includeFunctionalNecessity = true)
    {
        var effective = grading.ComputeEffectiveScore(includeFunctionalNecessity);
        return new(
            grading.Graded,
            effective?.FinalScore,
            effective?.RawPoints,
            effective?.RiskBand.ToString(),
            new CriterionScoreResponse(grading.CarcinogenicityScore, grading.CarcinogenicityNote),
            new CriterionScoreResponse(grading.BanStatusScore, grading.BanStatusNote),
            new CriterionScoreResponse(grading.AllergicReactionsScore, grading.AllergicReactionsNote),
            new CriterionScoreResponse(grading.CumulativeRiskScore, grading.CumulativeRiskNote),
            new CriterionScoreResponse(grading.OriginScore, grading.OriginNote),
            new CriterionScoreResponse(grading.ChildrenAndVulnerableScore, grading.ChildrenAndVulnerableNote),
            new CriterionScoreResponse(grading.FunctionalNecessityScore, grading.FunctionalNecessityNote));
    }
}

/// <summary>
/// Additive detail response containing comprehensive information about a specific additive, including its grading and related details.
/// </summary>
/// <param name="Id"></param>
/// <param name="ENumber"></param>
/// <param name="Name"></param>
/// <param name="NameVerbatim"></param>
/// <param name="RegulatoryNoteFromSource"></param>
/// <param name="Purpose"></param>
/// <param name="FoundIn"></param>
/// <param name="SourceOrigin"></param>
/// <param name="HealthConcerns"></param>
/// <param name="SideEffects"></param>
/// <param name="BannedAnywhere"></param>
/// <param name="CommonNamesAndSynonyms"></param>
/// <param name="EvidenceSources"></param>
/// <param name="LastResearched"></param>
/// <param name="Grading"></param>
public record AdditiveDetailResponse(
    int Id,
    string? ENumber,
    string Name,
    string NameVerbatim,
    string? RegulatoryNoteFromSource,
    string? Purpose,
    string? FoundIn,
    string? SourceOrigin,
    string? HealthConcerns,
    string? SideEffects,
    string? BannedAnywhere,
    IReadOnlyList<string> CommonNamesAndSynonyms,
    IReadOnlyList<string> EvidenceSources,
    DateOnly? LastResearched,
    SafetyGradingResponse? Grading)
{
    public static AdditiveDetailResponse FromEntity(Additive additive, bool includeFunctionalNecessity = true) => new(
        additive.Id,
        additive.ENumber,
        additive.Name,
        additive.NameVerbatim,
        additive.RegulatoryNoteFromSource,
        additive.Purpose,
        additive.FoundIn,
        additive.SourceOrigin,
        additive.HealthConcerns,
        additive.SideEffects,
        additive.BannedAnywhere,
        additive.CommonNamesAndSynonyms,
        additive.EvidenceSources,
        additive.LastResearched,
        additive.Grading is null ? null : SafetyGradingResponse.FromEntity(additive.Grading, includeFunctionalNecessity));
}
