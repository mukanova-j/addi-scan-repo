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
    /// Accepts an uploaded label photo, validates its type and size, extracts the ingredient
    /// text with OCR, then — once OCR succeeds — detects additives in that text and persists
    /// the scan (photo, text, and matches) to the caller's history in one step.
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

        using var buffer = new MemoryStream();
        await using (var fileStream = file.OpenReadStream())
        {
            await fileStream.CopyToAsync(buffer, cancellationToken);
        }

        var imageBytes = buffer.ToArray();
        var headerLength = Math.Min(SignatureHeaderLength, imageBytes.Length);
        var result = ImageUploadValidator.Validate(imageBytes.AsSpan(0, headerLength), file.ContentType, file.Length);

        if (!result.IsAccepted)
        {
            return Ok(new ImageUploadResponse(false, DescribeRejection(result.Reason), null, null, null, [], null, null));
        }

        await using var imageStream = new MemoryStream(imageBytes);
        var ocrResult = await textExtractionService.ExtractTextAsync(imageStream, cancellationToken);

        if (!ocrResult.Success || ocrResult.Text is null)
        {
            return Ok(new ImageUploadResponse(true, ocrResult.Error, result.Format.ToString(), null, null, [], null, null));
        }

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (record, detection) = await PersistScanAsync(userId, ocrResult.Text, imageBytes, file.ContentType, cancellationToken);

        return Ok(new ImageUploadResponse(
            true,
            null,
            result.Format.ToString(),
            ocrResult.Text,
            record.Id,
            detection.Matches.Select(DetectedAdditiveResponse.FromMatch).ToList(),
            detection.OverallRiskBand?.ToString(),
            detection.WorstMatch?.Additive.Name));
    }

    /// <summary>
    /// Detects additives in ingredient text (typically pasted or corrected text, without an
    /// accompanying photo) and returns each match's existing safety grading, plus the worst
    /// risk band among matches that are graded. Not rate-limited like upload: this is a cheap
    /// in-memory text match, not an expensive OCR pass. Also saves the scan to the caller's
    /// history via the same helper Upload uses, without a photo.
    /// </summary>
    [HttpPost("analyze")]
    public async Task<ActionResult<ScanAnalysisResponse>> Analyze(
        [FromBody] AnalyzeTextRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest("No text was provided.");
        }

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (_, result) = await PersistScanAsync(userId, request.Text, null, null, cancellationToken);

        return Ok(ScanAnalysisResponse.FromResult(result));
    }

    /// <summary>
    /// Returns full detail for one scan owned by the caller, or 404 if it doesn't exist or
    /// belongs to someone else.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ScanDetailResponse>> GetScan(Guid id, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var record = await dbContext.ScanRecords
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId, cancellationToken);

        if (record is null)
        {
            return NotFound();
        }

        var result = await DetectAsync(record, cancellationToken);

        return Ok(ScanDetailResponse.FromRecord(record.Id, record.ScannedAt, record.ExtractedText, record.PhotoData is not null, result));
    }

    /// <summary>
    /// Streams back the photo stored for one scan owned by the caller, or 404 if the scan
    /// doesn't exist, belongs to someone else, or has no stored photo.
    /// </summary>
    [HttpGet("{id:guid}/photo")]
    public async Task<IActionResult> GetScanPhoto(Guid id, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var record = await dbContext.ScanRecords
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId, cancellationToken);

        if (record?.PhotoData is null)
        {
            return NotFound();
        }

        return File(record.PhotoData, record.PhotoContentType ?? "application/octet-stream");
    }

    /// <summary>
    /// Detects additives for an already-persisted scan record by rejoining its stored matches
    /// against the current Additives table, same as GetHistory does per-record.
    /// </summary>
    private async Task<AdditiveDetectionResult> DetectAsync(ScanRecord record, CancellationToken cancellationToken)
    {
        var additiveIds = record.Matches.Select(m => m.AdditiveId).Distinct().ToList();
        var additivesById = await dbContext.Additives
            .Where(a => additiveIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, cancellationToken);

        var matches = record.Matches
            .Where(m => additivesById.ContainsKey(m.AdditiveId))
            .Select(m => new AdditiveMatch(additivesById[m.AdditiveId], m.MatchedTerm, m.Kind))
            .OrderBy(m => m.Additive.Id)
            .ToList();

        var worst = AdditiveDetector.PickWorst(matches);
        return new AdditiveDetectionResult(matches, worst?.Additive.Grading?.RiskBand, worst);
    }

    /// <summary>
    /// Runs additive detection against the current Additives table and persists a new
    /// ScanRecord (with an optional photo) plus its matches. Shared by Upload and Analyze so
    /// the detect-then-persist logic lives in one place.
    /// </summary>
    private async Task<(ScanRecord Record, AdditiveDetectionResult Result)> PersistScanAsync(
        Guid userId, string text, byte[]? photoData, string? photoContentType, CancellationToken cancellationToken)
    {
        var additives = await dbContext.Additives.ToListAsync(cancellationToken);
        var result = AdditiveDetector.Detect(text, additives);

        var record = new ScanRecord
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ExtractedText = text,
            ScannedAt = DateTime.UtcNow,
            PhotoData = photoData,
            PhotoContentType = photoContentType,
            Matches = result.Matches
                .Select(m => new ScanRecordMatch { Id = Guid.NewGuid(), AdditiveId = m.Additive.Id, MatchedTerm = m.MatchedTerm, Kind = m.Kind })
                .ToList(),
        };

        dbContext.ScanRecords.Add(record);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (record, result);
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
