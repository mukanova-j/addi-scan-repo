using AddiScan.Core.Detection;
using AddiScan.Core.Entities;
using AddiScan.Core.Grading;

namespace AddiScan.Tests;

public class AdditiveDetectorTests
{
    private static Additive MakeAdditive(
        int id,
        string name,
        string? eNumber = null,
        List<string>? synonyms = null,
        RiskBand? riskBand = null,
        decimal? finalScore = null,
        bool graded = true) => new()
    {
        Id = id,
        Name = name,
        NameVerbatim = name,
        ENumber = eNumber,
        CommonNamesAndSynonyms = synonyms ?? [],
        Grading = riskBand is null
            ? new SafetyGrading { Graded = false }
            : new SafetyGrading { Graded = graded, RiskBand = riskBand, FinalScore = finalScore },
    };

    [Fact]
    public void Detect_MatchesByName()
    {
        var additives = new[] { MakeAdditive(1, "Sodium Benzoate", riskBand: RiskBand.Moderate) };

        var result = AdditiveDetector.Detect("Water, Sodium Benzoate, Salt", additives);

        var match = Assert.Single(result.Matches);
        Assert.Equal(1, match.Additive.Id);
        Assert.Equal(MatchKind.Name, match.Kind);
    }

    [Fact]
    public void Detect_MatchesBySynonym()
    {
        var additives = new[] { MakeAdditive(1, "Sodium Benzoate", synonyms: ["Benzoate of Soda"], riskBand: RiskBand.Moderate) };

        var result = AdditiveDetector.Detect("Water, Benzoate of Soda, Salt", additives);

        var match = Assert.Single(result.Matches);
        Assert.Equal(MatchKind.Synonym, match.Kind);
        Assert.Equal("Benzoate of Soda", match.MatchedTerm);
    }

    [Theory]
    [InlineData("Preservative (E211)")]
    [InlineData("Preservative (E 211)")]
    [InlineData("Preservative (E-211)")]
    public void Detect_MatchesENumberInAnyWrittenForm(string text)
    {
        var additives = new[] { MakeAdditive(1, "Sodium Benzoate", eNumber: "E211") };

        var result = AdditiveDetector.Detect(text, additives);

        var match = Assert.Single(result.Matches);
        Assert.Equal(MatchKind.ENumber, match.Kind);
    }

    [Fact]
    public void Detect_IsCaseInsensitive()
    {
        var additives = new[] { MakeAdditive(1, "Sodium Benzoate") };

        var result = AdditiveDetector.Detect("SODIUM BENZOATE", additives);

        Assert.Single(result.Matches);
    }

    [Fact]
    public void Detect_DoesNotMatchSubstringInsideLongerWord()
    {
        var additives = new[] { MakeAdditive(1, "Salt") };

        var result = AdditiveDetector.Detect("Contains saltwater fish", additives);

        Assert.Empty(result.Matches);
    }

    [Fact]
    public void Detect_DedupesWhenMatchedByMultipleTerms()
    {
        var additives = new[] { MakeAdditive(1, "Sodium Benzoate", eNumber: "E211", synonyms: ["Benzoate of Soda"]) };

        var result = AdditiveDetector.Detect("Sodium Benzoate (E211), also known as Benzoate of Soda", additives);

        Assert.Single(result.Matches);
    }

    [Fact]
    public void Detect_ReturnsEmptyForNoMatches()
    {
        var additives = new[] { MakeAdditive(1, "Sodium Benzoate") };

        var result = AdditiveDetector.Detect("Water, Salt, Sugar", additives);

        Assert.Empty(result.Matches);
        Assert.Null(result.OverallRiskBand);
        Assert.Null(result.WorstMatch);
    }

    [Fact]
    public void Detect_OverallRiskBandPicksWorstGradedMatch()
    {
        var additives = new[]
        {
            MakeAdditive(1, "Sodium Benzoate", riskBand: RiskBand.Moderate, finalScore: 2.4m),
            MakeAdditive(2, "Tartrazine", riskBand: RiskBand.HighRisk, finalScore: 3.6m),
        };

        var result = AdditiveDetector.Detect("Sodium Benzoate and Tartrazine", additives);

        Assert.Equal(RiskBand.HighRisk, result.OverallRiskBand);
        Assert.Equal("Tartrazine", result.WorstMatch?.Additive.Name);
    }

    [Fact]
    public void Detect_OverallRiskBandIsNullWhenAllDetectedAreUngraded()
    {
        var additives = new[] { MakeAdditive(1, "Anthrapen", riskBand: null) };

        var result = AdditiveDetector.Detect("Contains Anthrapen", additives);

        Assert.Single(result.Matches);
        Assert.Null(result.OverallRiskBand);
        Assert.Null(result.WorstMatch);
    }

    [Fact]
    public void PickWorst_ReturnsHighestSeverityGradedMatch()
    {
        var moderate = new AdditiveMatch(MakeAdditive(1, "Sodium Benzoate", riskBand: RiskBand.Moderate, finalScore: 2.4m), "Sodium Benzoate", MatchKind.Name);
        var highRisk = new AdditiveMatch(MakeAdditive(2, "Tartrazine", riskBand: RiskBand.HighRisk, finalScore: 3.6m), "Tartrazine", MatchKind.Name);

        var worst = AdditiveDetector.PickWorst([moderate, highRisk]);

        Assert.Equal("Tartrazine", worst?.Additive.Name);
    }

    [Fact]
    public void PickWorst_ReturnsNullWhenNoneAreGraded()
    {
        var ungraded = new AdditiveMatch(MakeAdditive(1, "Anthrapen", riskBand: null), "Anthrapen", MatchKind.Name);

        Assert.Null(AdditiveDetector.PickWorst([ungraded]));
    }

    [Fact]
    public void PickWorst_BreaksSeverityTiesByFinalScoreThenId()
    {
        var a = new AdditiveMatch(MakeAdditive(2, "A", riskBand: RiskBand.HighRisk, finalScore: 3.2m), "A", MatchKind.Name);
        var b = new AdditiveMatch(MakeAdditive(1, "B", riskBand: RiskBand.HighRisk, finalScore: 3.9m), "B", MatchKind.Name);

        var worst = AdditiveDetector.PickWorst([a, b]);

        Assert.Equal("B", worst?.Additive.Name);
    }
}
