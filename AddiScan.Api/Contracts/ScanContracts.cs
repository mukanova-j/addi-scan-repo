namespace AddiScan.Api.Contracts;

/// <summary>
/// Response for an image upload validation check. This step only validates the upload;
/// OCR text extraction happens in a later step once the file passes validation.
/// </summary>
/// <param name="Accepted"></param>
/// <param name="Message"></param>
/// <param name="DetectedFormat"></param>
public record ImageUploadResponse(bool Accepted, string? Message, string? DetectedFormat);
