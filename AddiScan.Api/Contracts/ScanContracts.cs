using AddiScan.Core.Detection;

namespace AddiScan.Api.Contracts;

/// <summary>
/// Response for a label image upload. Accepted reflects whether the upload passed type/size/
/// signature validation. Message carries a rejection reason when Accepted is false, or an OCR
/// failure reason when Accepted is true but text extraction did not succeed. ExtractedText holds
/// the OCR result once validation and extraction both succeed.
/// </summary>
/// <param name="Accepted"></param>
/// <param name="Message"></param>
/// <param name="DetectedFormat"></param>
/// <param name="ExtractedText"></param>
public record ImageUploadResponse(bool Accepted, string? Message, string? DetectedFormat, string? ExtractedText);

/// <summary>
/// Request to detect additives in ingredient text, typically the ExtractedText from an upload.
/// </summary>
/// <param name="Text"></param>
public record AnalyzeTextRequest(string Text);

/// <summary>
/// A single additive detected in the analyzed text, along with its existing safety grading.
/// </summary>
/// <param name="Id"></param>
/// <param name="ENumber"></param>
/// <param name="Name"></param>
/// <param name="MatchedTerm"></param>
/// <param name="Graded"></param>
/// <param name="FinalScore"></param>
/// <param name="RiskBand"></param>
public record DetectedAdditiveResponse(
    int Id,
    string? ENumber,
    string Name,
    string MatchedTerm,
    bool Graded,
    decimal? FinalScore,
    string? RiskBand)
{
    public static DetectedAdditiveResponse FromMatch(AdditiveMatch match) => new(
        match.Additive.Id,
        match.Additive.ENumber,
        match.Additive.Name,
        match.MatchedTerm,
        match.Additive.Grading?.Graded ?? false,
        match.Additive.Grading?.FinalScore,
        match.Additive.Grading?.RiskBand?.ToString());
}

/// <summary>
/// Result of analyzing ingredient text for additives. OverallRiskBand and WorstAdditiveName
/// reflect the worst-graded additive detected, or null when nothing detected is graded yet.
/// </summary>
/// <param name="Detected"></param>
/// <param name="OverallRiskBand"></param>
/// <param name="WorstAdditiveName"></param>
public record ScanAnalysisResponse(
    List<DetectedAdditiveResponse> Detected,
    string? OverallRiskBand,
    string? WorstAdditiveName)
{
    public static ScanAnalysisResponse FromResult(AdditiveDetectionResult result) => new(
        result.Matches.Select(DetectedAdditiveResponse.FromMatch).ToList(),
        result.OverallRiskBand?.ToString(),
        result.WorstMatch?.Additive.Name);
}
