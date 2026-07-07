using System.Text.Json;
using AddiScan.Core.Entities;
using AddiScan.Core.Grading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AddiScan.Infrastructure.Persistence.Seed;

/// <summary>
/// Imports addiscan_additives.json into the Additives table on startup. The dataset file doesn't ship with
/// the repo yet, so a missing path is expected and this no-ops rather than failing startup.
/// </summary>
public class AdditiveDataSeeder(AddiScanDbContext dbContext, ILogger<AdditiveDataSeeder> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public async Task SeedAsync(string datasetPath, CancellationToken cancellationToken = default)
    {
        if (await dbContext.Additives.AnyAsync(cancellationToken))
        {
            return;
        }

        if (!File.Exists(datasetPath))
        {
            logger.LogInformation("Additive dataset not found at {Path}; skipping seed.", datasetPath);
            return;
        }

        await using var stream = File.OpenRead(datasetPath);
        var dataset = await JsonSerializer.DeserializeAsync<AdditiveDatasetDto>(stream, JsonOptions, cancellationToken);
        if (dataset is null)
        {
            logger.LogWarning("Additive dataset at {Path} could not be parsed.", datasetPath);
            return;
        }

        foreach (var dto in dataset.Additives)
        {
            dbContext.Additives.Add(MapToEntity(dto));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded {Count} additives from {Path}.", dataset.Additives.Count, datasetPath);
    }

    private static Additive MapToEntity(AdditiveDto dto) => new()
    {
        Id = dto.Id,
        ENumber = dto.ENumber,
        Name = dto.Name,
        NameVerbatim = dto.NameVerbatim,
        RegulatoryNoteFromSource = dto.RegulatoryNoteFromSource,
        Purpose = dto.Purpose,
        FoundIn = dto.FoundIn,
        SourceOrigin = dto.SourceOrigin,
        HealthConcerns = dto.HealthConcerns,
        SideEffects = dto.SideEffects,
        BannedAnywhere = dto.BannedAnywhere,
        CommonNamesAndSynonyms = dto.CommonNamesAndSynonyms,
        EvidenceSources = dto.EvidenceSources,
        LastResearched = dto.LastResearched,
        Grading = MapGrading(dto.SafetyGrading),
    };

    private static SafetyGrading? MapGrading(SafetyGradingDto? dto)
    {
        if (dto is null)
        {
            return null;
        }

        return new SafetyGrading
        {
            Graded = dto.Graded,
            FinalScore = dto.FinalScore,
            RawPoints = dto.RawPoints,
            RiskBand = dto.RiskBand is null ? null : Enum.Parse<RiskBand>(dto.RiskBand, ignoreCase: true),
            CarcinogenicityScore = dto.Criteria?.Carcinogenicity?.Score,
            CarcinogenicityNote = dto.Criteria?.Carcinogenicity?.Note,
            BanStatusScore = dto.Criteria?.BanStatus?.Score,
            BanStatusNote = dto.Criteria?.BanStatus?.Note,
            AllergicReactionsScore = dto.Criteria?.AllergicReactions?.Score,
            AllergicReactionsNote = dto.Criteria?.AllergicReactions?.Note,
            CumulativeRiskScore = dto.Criteria?.CumulativeRisk?.Score,
            CumulativeRiskNote = dto.Criteria?.CumulativeRisk?.Note,
            OriginScore = dto.Criteria?.Origin?.Score,
            OriginNote = dto.Criteria?.Origin?.Note,
            ChildrenAndVulnerableScore = dto.Criteria?.ChildrenAndVulnerable?.Score,
            ChildrenAndVulnerableNote = dto.Criteria?.ChildrenAndVulnerable?.Note,
            FunctionalNecessityScore = dto.Criteria?.FunctionalNecessity?.Score,
            FunctionalNecessityNote = dto.Criteria?.FunctionalNecessity?.Note,
        };
    }
}
