using System.Security.Claims;
using AddiScan.Api.Contracts;
using AddiScan.Api.Ocr;
using AddiScan.Core.Detection;
using AddiScan.Core.History;
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
    /// Also saves the scan to the caller's history — only the extracted text and which
    /// additives/terms matched, never a grading snapshot, so history stays live (see
    /// GetHistory).
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

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        dbContext.ScanRecords.Add(new ScanRecord
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ExtractedText = request.Text,
            ScannedAt = DateTime.UtcNow,
            Matches = result.Matches
                .Select(m => new ScanRecordMatch { Id = Guid.NewGuid(), AdditiveId = m.Additive.Id, MatchedTerm = m.MatchedTerm, Kind = m.Kind })
                .ToList(),
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ScanAnalysisResponse.FromResult(result));
    }

    /// <summary>
    /// Lists the authenticated user's past scans, newest first. Detected additives are
    /// rejoined against the current Additives table rather than replayed from a stored
    /// grading snapshot, so history always reflects the latest evidence for each additive.
    /// </summary>
    [HttpGet("history")]
    public async Task<ActionResult<List<ScanHistoryItemResponse>>> GetHistory(CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var records = await dbContext.ScanRecords
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.ScannedAt)
            .ToListAsync(cancellationToken);

        var additiveIds = records.SelectMany(r => r.Matches.Select(m => m.AdditiveId)).Distinct().ToList();
        var additivesById = await dbContext.Additives
            .Where(a => additiveIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, cancellationToken);

        var response = records.Select(record =>
        {
            var matches = record.Matches
                .Where(m => additivesById.ContainsKey(m.AdditiveId))
                .Select(m => new AdditiveMatch(additivesById[m.AdditiveId], m.MatchedTerm, m.Kind))
                .OrderBy(m => m.Additive.Id)
                .ToList();

            var worst = AdditiveDetector.PickWorst(matches);
            var result = new AdditiveDetectionResult(matches, worst?.Additive.Grading?.RiskBand, worst);

            return ScanHistoryItemResponse.FromRecord(record.Id, record.ScannedAt, record.ExtractedText, result);
        }).ToList();

        return Ok(response);
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
