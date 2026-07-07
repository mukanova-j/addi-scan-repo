namespace AddiScan.Infrastructure.Persistence.Seed;

public class AdditiveDatasetDto
{
    public List<AdditiveDto> Additives { get; set; } = [];
}

/// <summary>
/// Additive data transfer object (DTO) representing the structure of additive information in the dataset.
/// </summary>
public class AdditiveDto
{
    public int Id { get; set; }
    /// <summary>
    /// Additive E number, if applicable. This is a unique identifier for food additives used in the European Union.
    /// </summary>
    public string? ENumber { get; set; }
    /// <summary>
    /// The official name of the additive. 
    /// This is a required field and should be provided in the dataset.
    /// </summary>
    public required string Name { get; set; }
    /// <summary>
    /// Name of the additive as it appears in the source dataset, preserving original formatting and spelling.
    /// This is a required field and should be provided in the dataset.
    /// </summary>
    public required string NameVerbatim { get; set; }
    /// <summary>
    /// Regulatory note or comment from the source dataset regarding the additive. 
    /// This may include information about its approval status, restrictions, or other regulatory considerations.
    /// </summary>
    public string? RegulatoryNoteFromSource { get; set; }
    /// <summary>
    /// Purpose or intended use of the additive.
    /// </summary>
    public string? Purpose { get; set; }
    /// <summary>
    /// Places or products where the additive is commonly found. This may include food categories, beverages, or other consumer products.
    /// </summary>
    public string? FoundIn { get; set; }
    /// <summary>
    /// Origin of the additive, including its source (e.g., natural, synthetic).
    /// </summary>
    public string? SourceOrigin { get; set; }
    /// <summary>
    /// Health concerns associated with the additive, if any. 
    /// This may include potential risks or adverse effects on health.
    /// </summary>
    public string? HealthConcerns { get; set; }
    /// <summary>
    /// Side effects associated with the additive, if any. 
    /// This may include information about allergic reactions, sensitivities, or other adverse effects.
    /// </summary>
    public string? SideEffects { get; set; }
    /// <summary>
    /// Indicates whether the additive is banned in any countries or regions.
    /// </summary>
    public string? BannedAnywhere { get; set; }
    /// <summary>
    /// List of common names and synonyms for the additive. 
    /// This may include alternative names, trade names, or other identifiers used in different contexts.
    /// </summary>
    public List<string> CommonNamesAndSynonyms { get; set; } = [];
    /// <summary>
    /// List of evidence sources or references that provide information about the additive.
    /// </summary>
    public List<string> EvidenceSources { get; set; } = [];
    /// <summary>
    /// Last researched date for the additive, indicating when the information was last reviewed or updated.
    /// </summary>
    public DateOnly? LastResearched { get; set; }
    /// <summary>
    /// Safety grading information for the additive, including scores and risk assessments based on various criteria.
    /// </summary>
    public SafetyGradingDto? SafetyGrading { get; set; }
}

/// <summary>
/// Safety grading data transfer object (DTO) representing the safety assessment of an additive based on various criteria.
/// </summary>
public class SafetyGradingDto
{
    /// <summary>
    /// Flag indicating whether the additive has been graded for safety.
    /// If true, the grading information is available; otherwise, it may be incomplete or unavailable.
    /// </summary>
    public bool Graded { get; set; }
    /// <summary>
    /// Final safety score for the additive, calculated based on the individual criterion scores.
    /// </summary>
    public decimal? FinalScore { get; set; }
    /// <summary>
    /// Raw points assigned to the additive based on the safety assessment criteria. 
    /// This may be used to calculate the final score and determine the risk band.
    /// </summary>
    public int? RawPoints { get; set; }
    /// <summary>
    /// Risk band classification for the additive, indicating its overall safety level based on the final score and grading criteria.
    /// </summary>
    public string? RiskBand { get; set; }
    /// <summary>
    /// Grading criteria and scores for the additive, including individual assessments for:
    /// * carcinogenicity
    /// * ban status
    /// * allergic reactions
    /// * cumulative risk
    /// * origin
    /// * children and vulnerable populations
    /// * functional necessity.
    /// </summary>
    public CriteriaDto? Criteria { get; set; }
}

/// <summary>
/// Criteria data transfer object (DTO) representing the individual safety assessment criteria for an additive, 
/// including scores and notes for each criterion.
/// </summary>
public class CriteriaDto
{
    /// <summary>
    /// Carcinogenicity — max 4 pts
    /// Scores follow IARC classification.
    /// Under the permissive approach, animal-only evidence without IARC Group 2B or higher scores at most 1 pt.
    /// </summary>
    public CriterionScoreDto? Carcinogenicity { get; set; }
    /// <summary>
    /// Ban Status — max 3 pts
    /// Count outright bans(not just restricted-use approvals) in sovereign countries or major regulatory blocs(EU, USA, etc.).
    /// </summary>
    public CriterionScoreDto? BanStatus { get; set; }
    /// <summary>
    /// Allergic / Sensitivity Reactions — max 2 pts
    /// Assess whether reactions are documented in scientific literature for the general population or only for individuals with known sensitivities.
    /// </summary>
    public CriterionScoreDto? AllergicReactions { get; set; }
    /// <summary>
    /// Cumulative / Long-Term Exposure Risk — max 2 pts
    /// Assess whether real-world dietary patterns lead to ADI exceedances across population groups.
    /// </summary>
    public CriterionScoreDto? CumulativeRisk { get; set; }
    /// <summary>
    /// Origin — max 1 pt
    /// Assess the synthetic origin of the additive. Natural or biologically-derived additives start from a lower baseline of structural toxicology concern.
    /// </summary>
    public CriterionScoreDto? Origin { get; set; }
    /// <summary>
    /// Effect on Children & Vulnerable Groups — max 2 pts
    /// Assess whether there is specific documented harm beyond what applies to the general adult population.
    /// </summary>
    public CriterionScoreDto? ChildrenAndVulnerable { get; set; }
    /// <summary>
    /// Functional Necessity — modifier −1 to +1
    /// This is the only criterion that can lower a score.
    /// It reflects the real-world trade-off between risk and the safety or preservation function the additive provides.
    /// </summary>
    public CriterionScoreDto? FunctionalNecessity { get; set; }
}

public class CriterionScoreDto
{
    /// <summary>
    /// Score assigned to the additive for this specific criterion, based on the assessment of its safety and risk factors.
    /// </summary>
    public int? Score { get; set; }
    /// <summary>
    /// Note or comment providing additional context or explanation for the score assigned to the additive for this specific criterion.
    /// </summary>
    public string? Note { get; set; }
}
