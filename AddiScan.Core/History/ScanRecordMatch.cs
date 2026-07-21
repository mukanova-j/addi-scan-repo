using AddiScan.Core.Detection;

namespace AddiScan.Core.History;

/// <summary>
/// One additive detected within a ScanRecord. Deliberately mirrors AdditiveMatch's shape
/// (additive id + matched term + match kind) rather than storing the additive's name,
/// E-number, or grading — those get looked up fresh from the current Additives table
/// whenever history is read, so a later re-grade of an additive is reflected retroactively.
/// </summary>
public class ScanRecordMatch
{
    public Guid Id { get; set; }
    public required int AdditiveId { get; set; }
    public required string MatchedTerm { get; set; }
    public required MatchKind Kind { get; set; }
}
