namespace AddiScan.Core.History;

/// <summary>
/// One scan analysis a user ran: the extracted ingredient text and which additives were
/// detected in it at the time. Stores only enough to re-derive the match later (additive id,
/// matched term, match kind) — never a frozen grading snapshot — so history always reflects
/// the current Additives dataset when read. No uploaded image is stored, per data minimization.
/// </summary>
public class ScanRecord
{
    public Guid Id { get; set; }
    public required Guid UserId { get; set; }
    public required string ExtractedText { get; set; }
    public DateTime ScannedAt { get; set; }

    public List<ScanRecordMatch> Matches { get; set; } = [];
}
