namespace AddiScan.Core.History;

/// <summary>
/// One scan analysis a user ran: the extracted ingredient text, the uploaded label photo, and
/// which additives were detected in the text at the time. Matches store only enough to
/// re-derive the match later (additive id, matched term, match kind) — never a frozen grading
/// snapshot — so history always reflects the current Additives dataset when read.
/// </summary>
public class ScanRecord
{
    public Guid Id { get; set; }
    public required Guid UserId { get; set; }
    public required string ExtractedText { get; set; }
    public DateTime ScannedAt { get; set; }
    public byte[]? PhotoData { get; set; }
    public string? PhotoContentType { get; set; }

    public List<ScanRecordMatch> Matches { get; set; } = [];
}
