namespace AddiScan.Core.Entities;

public class Additive
{
    public int Id { get; set; }
    public string? ENumber { get; set; }
    public required string Name { get; set; }
    public required string NameVerbatim { get; set; }
    public string? RegulatoryNoteFromSource { get; set; }

    public string? Purpose { get; set; }
    public string? FoundIn { get; set; }
    public string? SourceOrigin { get; set; }
    public string? HealthConcerns { get; set; }
    public string? SideEffects { get; set; }
    public string? BannedAnywhere { get; set; }
    public List<string> CommonNamesAndSynonyms { get; set; } = [];
    public List<string> EvidenceSources { get; set; } = [];
    public DateOnly? LastResearched { get; set; }

    public SafetyGrading? Grading { get; set; }
}
