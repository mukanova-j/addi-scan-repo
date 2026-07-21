using AddiScan.Api.Contracts;
using AddiScan.Api.Ocr;
using AddiScan.Core.Detection;
using AddiScan.Core.Uploads;
using AddiScan.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace AddiScan.Api.Controllers;

[ApiController]
[Route("api/scan")]
[Authorize]
public class ScanController(ITextExtractionService textExtractionService, AddiScanDbContext dbContext) : ControllerBase
{
    private const int SignatureHeaderLength = 12;

    /// <summary>
    /// Accepts an uploaded label photo, validates its type and size, then extracts the
    /// ingredient text with OCR once validation passes. 
    /// Requires an authenticated user.
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

    /// <summary>
    /// Detects additives in ingredient text (typically the ExtractedText from an upload) and
    /// returns each match's existing safety grading, plus the worst risk band among matches
    /// that are graded. Not rate-limited like upload: this is a cheap in-memory text match,
    /// not an expensive OCR pass, so re-analyzing edited text shouldn't be throttled.
    /// </summary>
    [HttpPost("analyze")]
    public async Task<ActionResult<ScanAnalysisResponse>> Analyze(
        [FromBody] AnalyzeTextRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest("No text was provided.");
        }

        var additives = await dbContext.Additives.ToListAsync(cancellationToken);
        var result = AdditiveDetector.Detect(request.Text, additives);

        return Ok(ScanAnalysisResponse.FromResult(result));
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
