using AddiScan.Core.Uploads;

namespace AddiScan.Tests;

public class ImageUploadValidatorTests
{
    private static readonly byte[] JpegHeader = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01];
    private static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];
    private static readonly byte[] WebpHeader = [0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50];

    [Fact]
    public void Validate_AcceptsValidJpeg()
    {
        var result = ImageUploadValidator.Validate(JpegHeader, "image/jpeg", 1000);

        Assert.True(result.IsAccepted);
        Assert.Equal(DetectedImageFormat.Jpeg, result.Format);
    }

    [Fact]
    public void Validate_AcceptsValidPng()
    {
        var result = ImageUploadValidator.Validate(PngHeader, "image/png", 1000);

        Assert.True(result.IsAccepted);
        Assert.Equal(DetectedImageFormat.Png, result.Format);
    }

    [Fact]
    public void Validate_AcceptsValidWebp()
    {
        var result = ImageUploadValidator.Validate(WebpHeader, "image/webp", 1000);

        Assert.True(result.IsAccepted);
        Assert.Equal(DetectedImageFormat.Webp, result.Format);
    }

    [Fact]
    public void Validate_RejectsEmptyFile()
    {
        var result = ImageUploadValidator.Validate(JpegHeader, "image/jpeg", 0);

        Assert.False(result.IsAccepted);
        Assert.Equal(ImageUploadRejectionReason.EmptyFile, result.Reason);
    }

    [Fact]
    public void Validate_RejectsFileOverSizeLimit()
    {
        var result = ImageUploadValidator.Validate(JpegHeader, "image/jpeg", ImageUploadValidator.MaxFileSizeBytes + 1);

        Assert.False(result.IsAccepted);
        Assert.Equal(ImageUploadRejectionReason.FileTooLarge, result.Reason);
    }

    [Fact]
    public void Validate_AcceptsFileAtExactSizeLimit()
    {
        var result = ImageUploadValidator.Validate(JpegHeader, "image/jpeg", ImageUploadValidator.MaxFileSizeBytes);

        Assert.True(result.IsAccepted);
    }

    [Fact]
    public void Validate_RejectsUnsupportedContentType()
    {
        var result = ImageUploadValidator.Validate(JpegHeader, "image/gif", 1000);

        Assert.False(result.IsAccepted);
        Assert.Equal(ImageUploadRejectionReason.UnsupportedContentType, result.Reason);
    }

    [Fact]
    public void Validate_RejectsNullContentType()
    {
        var result = ImageUploadValidator.Validate(JpegHeader, null, 1000);

        Assert.False(result.IsAccepted);
        Assert.Equal(ImageUploadRejectionReason.UnsupportedContentType, result.Reason);
    }

    [Fact]
    public void Validate_RejectsSpoofedContentType()
    {
        // Declared as PNG but the bytes are actually a JPEG signature.
        var result = ImageUploadValidator.Validate(JpegHeader, "image/png", 1000);

        Assert.False(result.IsAccepted);
        Assert.Equal(ImageUploadRejectionReason.SignatureMismatch, result.Reason);
    }

    [Fact]
    public void Validate_RejectsNonImageContent()
    {
        var textHeader = "Hello world!"u8.ToArray();

        var result = ImageUploadValidator.Validate(textHeader, "image/jpeg", 1000);

        Assert.False(result.IsAccepted);
        Assert.Equal(ImageUploadRejectionReason.SignatureMismatch, result.Reason);
    }

    [Fact]
    public void Validate_RejectsHeaderShorterThanSignature_WithoutThrowing()
    {
        byte[] shortHeader = [0xFF, 0xD8, 0x00];

        var result = ImageUploadValidator.Validate(shortHeader, "image/jpeg", 1000);

        Assert.False(result.IsAccepted);
        Assert.Equal(ImageUploadRejectionReason.SignatureMismatch, result.Reason);
    }
}
