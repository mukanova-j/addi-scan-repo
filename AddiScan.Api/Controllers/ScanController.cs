using AddiScan.Api.Contracts;
using AddiScan.Core.Uploads;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AddiScan.Api.Controllers;

[ApiController]
[Route("api/scan")]
public class ScanController : ControllerBase
{
    private const int SignatureHeaderLength = 12;

    /// <summary>
    /// Accepts an uploaded label photo and validates its type and size before OCR processing.
    /// This step only validates the upload; OCR text extraction is a later step.
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
        await using (var stream = file.OpenReadStream())
        {
            bytesRead = await stream.ReadAtLeastAsync(header, SignatureHeaderLength, throwOnEndOfStream: false, cancellationToken);
        }

        var result = ImageUploadValidator.Validate(header.AsSpan(0, bytesRead), file.ContentType, file.Length);

        return Ok(new ImageUploadResponse(
            result.IsAccepted,
            result.IsAccepted ? null : DescribeRejection(result.Reason),
            result.IsAccepted ? result.Format.ToString() : null));
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
