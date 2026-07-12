namespace AddiScan.Core.Uploads;

public enum DetectedImageFormat
{
    Unknown,
    Jpeg,
    Png,
    Webp,
}

public enum ImageUploadRejectionReason
{
    None,
    EmptyFile,
    FileTooLarge,
    UnsupportedContentType,
    SignatureMismatch,
}

public record ImageValidationResult(bool IsAccepted, DetectedImageFormat Format, ImageUploadRejectionReason Reason)
{
    public static ImageValidationResult Accepted(DetectedImageFormat format) =>
        new(true, format, ImageUploadRejectionReason.None);

    public static ImageValidationResult Rejected(ImageUploadRejectionReason reason) =>
        new(false, DetectedImageFormat.Unknown, reason);
}

/// <summary>
/// Validates an uploaded label image before it reaches OCR processing. Takes raw header bytes
/// rather than a filename, because both the declared Content-Type and the file extension are
/// attacker-controlled and prove nothing about the actual file contents, only the magic-byte
/// signature does.
/// </summary>
public static class ImageUploadValidator
{
    /// <summary>
    /// Maximum allowed file size for uploaded images, in bytes (10 MB).
    /// </summary>
    public const long MaxFileSizeBytes = 10 * 1024 * 1024;

    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] RiffTag = "RIFF"u8.ToArray();
    private static readonly byte[] WebpTag = "WEBP"u8.ToArray();

    private static readonly IReadOnlyDictionary<string, DetectedImageFormat> AllowedContentTypes =
        new Dictionary<string, DetectedImageFormat>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = DetectedImageFormat.Jpeg,
            ["image/png"] = DetectedImageFormat.Png,
            ["image/webp"] = DetectedImageFormat.Webp,
        };

    /// <summary>
    /// Validates an uploaded image file based on its header bytes, declared content type, and length.
    /// </summary>
    /// <param name="header"></param>
    /// <param name="declaredContentType"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public static ImageValidationResult Validate(ReadOnlySpan<byte> header, string? declaredContentType, long length)
    {
        if (length <= 0)
        {
            return ImageValidationResult.Rejected(ImageUploadRejectionReason.EmptyFile);
        }

        if (length > MaxFileSizeBytes)
        {
            return ImageValidationResult.Rejected(ImageUploadRejectionReason.FileTooLarge);
        }

        if (declaredContentType is null || !AllowedContentTypes.TryGetValue(declaredContentType, out var declaredFormat))
        {
            return ImageValidationResult.Rejected(ImageUploadRejectionReason.UnsupportedContentType);
        }

        var sniffedFormat = SniffFormat(header);
        if (sniffedFormat == DetectedImageFormat.Unknown || sniffedFormat != declaredFormat)
        {
            return ImageValidationResult.Rejected(ImageUploadRejectionReason.SignatureMismatch);
        }

        return ImageValidationResult.Accepted(sniffedFormat);
    }

    /// <summary>
    /// Sniffs the image format from the header bytes by checking for known magic-byte signatures.
    /// </summary>
    /// <param name="header"></param>
    /// <returns></returns>
    private static DetectedImageFormat SniffFormat(ReadOnlySpan<byte> header)
    {
        if (StartsWith(header, JpegSignature))
        {
            return DetectedImageFormat.Jpeg;
        }

        if (StartsWith(header, PngSignature))
        {
            return DetectedImageFormat.Png;
        }

        if (header.Length >= 12 && StartsWith(header, RiffTag) && header[8..12].SequenceEqual(WebpTag))
        {
            return DetectedImageFormat.Webp;
        }

        return DetectedImageFormat.Unknown;
    }

    /// <summary>
    /// Checks if the header starts with the given signature bytes.
    /// </summary>
    /// <param name="header"></param>
    /// <param name="signature"></param>
    /// <returns></returns>
    private static bool StartsWith(ReadOnlySpan<byte> header, byte[] signature) =>
        header.Length >= signature.Length && header[..signature.Length].SequenceEqual(signature);
}
