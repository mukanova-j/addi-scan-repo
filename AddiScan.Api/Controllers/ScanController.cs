using AddiScan.Api.Contracts;
using AddiScan.Api.Ocr;
using AddiScan.Core.Uploads;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AddiScan.Api.Controllers;

[ApiController]
[Route("api/scan")]
public class ScanController(ITextExtractionService textExtractionService) : ControllerBase
{
    private const int SignatureHeaderLength = 12;

    /// <summary>
    /// Accepts an uploaded label photo, validates its type and size, then extracts the
    /// ingredient text with OCR once validation passes.
    /// </summary>
    /// <param name="file"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("upload")]
    [EnableRateLimiting("ScanUpload")]
    public async Task<ActionResult<ImageUploadResponse>> Upload(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("No file was uploaded.");
        }

        var header = new byte[SignatureHeaderLength];
        int bytesRead;
        await using (var headerStream = file.OpenReadStream())
        {
            bytesRead = await headerStream.ReadAtLeastAsync(header, SignatureHeaderLength, throwOnEndOfStream: false, cancellationToken);
        }

        var result = ImageUploadValidator.Validate(header.AsSpan(0, bytesRead), file.ContentType, file.Length);

        if (!result.IsAccepted)
        {
            return Ok(new ImageUploadResponse(false, DescribeRejection(result.Reason), null, null));
        }

        await using var imageStream = file.OpenReadStream();
        var ocrResult = await textExtractionService.ExtractTextAsync(imageStream, cancellationToken);

        return Ok(new ImageUploadResponse(
            true,
            ocrResult.Success ? null : ocrResult.Error,
            result.Format.ToString(),
            ocrResult.Text));
    }

    private static string DescribeRejection(ImageUploadRejectionReason reason) => reason switch
    {
        ImageUploadRejectionReason.EmptyFile => "The uploaded file is empty.",
        ImageUploadRejectionReason.FileTooLarge => "File exceeds the 10 MB limit.",
        ImageUploadRejectionReason.UnsupportedContentType => "Only JPEG, PNG, and WEBP images are supported.",
        ImageUploadRejectionReason.SignatureMismatch => "The file content does not match a supported image format.",
        _ => "The upload was rejected.",
    };
}
