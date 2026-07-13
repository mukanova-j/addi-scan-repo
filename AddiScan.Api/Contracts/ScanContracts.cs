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
