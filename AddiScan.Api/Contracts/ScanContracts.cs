using AddiScan.Core.Detection;

namespace AddiScan.Api.Contracts;

/// <summary>
/// Response for a label image upload. Accepted reflects whether the upload passed type/size/
/// signature validation. Message carries a rejection reason when Accepted is false, or an OCR
/// failure reason when Accepted is true but text extraction did not succeed. ExtractedText holds
/// the OCR result once validation and extraction both succeed, at which point the scan is also
/// persisted to history (with the photo) and ScanId/Detected/OverallRiskBand/WorstAdditiveName
/// are populated so the caller can jump straight to that scan's detail page.
/// </summary>
/// <param name="Accepted"></param>
/// <param name="Message"></param>
/// <param name="DetectedFormat"></param>
/// <param name="ExtractedText"></param>
/// <param name="ScanId"></param>
/// <param name="Detected"></param>
/// <param name="OverallRiskBand"></param>
/// <param name="WorstAdditiveName"></param>
public record ImageUploadResponse(
    bool Accepted,
    string? Message,
    string? DetectedFormat,
    string? ExtractedText,
    Guid? ScanId,
    List<DetectedAdditiveResponse> Detected,
    string? OverallRiskBand,
    string? WorstAdditiveName);

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
    public static DetectedAdditiveResponse FromMatch(AdditiveMatch match, bool includeFunctionalNecessity = true)
    {
        var effective = match.Additive.Grading?.ComputeEffectiveScore(includeFunctionalNecessity);
        return new(
            match.Additive.Id,
            match.Additive.ENumber,
            match.Additive.Name,
            match.MatchedTerm,
            match.Additive.Grading?.Graded ?? false,
            effective?.FinalScore,
            effective?.RiskBand.ToString());
    }
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
    public static ScanAnalysisResponse FromResult(AdditiveDetectionResult result, bool includeFunctionalNecessity = true) => new(
        result.Matches.Select(m => DetectedAdditiveResponse.FromMatch(m, includeFunctionalNecessity)).ToList(),
        result.OverallRiskBand?.ToString(),
        result.WorstMatch?.Additive.Name);
}

/// <summary>
/// One entry in a user's scan history. Detected/OverallRiskBand/WorstAdditiveName are
/// recomputed live from the current Additives table when history is read, not frozen at
/// scan time, so a later re-grade of an additive is reflected retroactively.
/// </summary>
/// <param name="Id"></param>
/// <param name="ScannedAt"></param>
/// <param name="ExtractedText"></param>
/// <param name="Detected"></param>
/// <param name="OverallRiskBand"></param>
/// <param name="WorstAdditiveName"></param>
public record ScanHistoryItemResponse(
    Guid Id,
    DateTime ScannedAt,
    string ExtractedText,
    List<DetectedAdditiveResponse> Detected,
    string? OverallRiskBand,
    string? WorstAdditiveName)
{
    public static ScanHistoryItemResponse FromRecord(
        Guid id, DateTime scannedAt, string text, AdditiveDetectionResult result, bool includeFunctionalNecessity = true) => new(
        id,
        scannedAt,
        text,
        result.Matches.Select(m => DetectedAdditiveResponse.FromMatch(m, includeFunctionalNecessity)).ToList(),
        result.OverallRiskBand?.ToString(),
        result.WorstMatch?.Additive.Name);
}

/// <summary>
/// Full detail for a single scan, scoped to its owning user. HasPhoto tells the caller
/// whether it's worth calling the photo endpoint at all. Detected/OverallRiskBand/
/// WorstAdditiveName are recomputed live from the current Additives table, same as history.
/// </summary>
/// <param name="Id"></param>
/// <param name="ScannedAt"></param>
/// <param name="ExtractedText"></param>
/// <param name="HasPhoto"></param>
/// <param name="Detected"></param>
/// <param name="OverallRiskBand"></param>
/// <param name="WorstAdditiveName"></param>
public record ScanDetailResponse(
    Guid Id,
    DateTime ScannedAt,
    string ExtractedText,
    bool HasPhoto,
    List<DetectedAdditiveResponse> Detected,
    string? OverallRiskBand,
    string? WorstAdditiveName)
{
    public static ScanDetailResponse FromRecord(
        Guid id, DateTime scannedAt, string text, bool hasPhoto, AdditiveDetectionResult result, bool includeFunctionalNecessity = true) => new(
        id,
        scannedAt,
        text,
        hasPhoto,
        result.Matches.Select(m => DetectedAdditiveResponse.FromMatch(m, includeFunctionalNecessity)).ToList(),
        result.OverallRiskBand?.ToString(),
        result.WorstMatch?.Additive.Name);
}
