namespace AddiScan.Api.Ocr;

/// <summary>
/// Result of an OCR text extraction attempt. Kept separate from success/failure of the HTTP
/// call itself, since a validated, well-formed image can still fail to yield readable text.
/// </summary>
/// <param name="Success"></param>
/// <param name="Text"></param>
/// <param name="Error"></param>
public record OcrResult(bool Success, string? Text, string? Error)
{
    public static OcrResult Ok(string text) => new(true, text, null);

    public static OcrResult Failed(string error) => new(false, null, error);
}

/// <summary>
/// Extracts ingredient text from a label photo. Implementations run OCR against already
/// validated image bytes; callers are responsible for type/size/signature validation first.
/// </summary>
public interface ITextExtractionService
{
    Task<OcrResult> ExtractTextAsync(Stream imageStream, CancellationToken cancellationToken);
}
