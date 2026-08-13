using System.Text;
using System.Text.RegularExpressions;
using AddiScan.Core.Entities;
using AddiScan.Core.Grading;

namespace AddiScan.Core.Detection;

public enum MatchKind
{
    Name,
    Synonym,
    ENumber,
}

public record AdditiveMatch(Additive Additive, string MatchedTerm, MatchKind Kind);

public record AdditiveDetectionResult(
    IReadOnlyList<AdditiveMatch> Matches,
    RiskBand? OverallRiskBand,
    AdditiveMatch? WorstMatch);

/// <summary>
/// Matches ingredient text extracted by OCR against a set of known additives, by name,
/// synonym, or E-number. Pure and DB-free so it stays unit-testable in isolation; callers
/// supply the candidate additives (typically all rows from the Additives table).
/// </summary>
public static class AdditiveDetector
{
    // Matches E211, E 211, and E-211 alike; group 1 is the digits plus optional suffix letter.
    private static readonly Regex ENumberPattern = new(
        @"\bE[\s-]?(\d{3,4}[a-dA-D]?)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static AdditiveDetectionResult Detect(
        string text, IEnumerable<Additive> candidates, bool includeFunctionalNecessity = true)
    {
        var candidateList = candidates as IReadOnlyList<Additive> ?? candidates.ToList();

        if (string.IsNullOrWhiteSpace(text) || candidateList.Count == 0)
        {
            return new AdditiveDetectionResult([], null, null);
        }

        // Padded so a plain Contains(" term ") is word-boundary-safe without regex.
        var haystack = $" {Normalize(text)} ";
        // Keyed by additive Id so one additive never appears twice even if several of its
        // terms (name, synonym, E-number) all match.
        var matches = new Dictionary<int, AdditiveMatch>();

        foreach (var additive in candidateList)
        {
            var match = FindNameOrSynonymMatch(additive, haystack);
            if (match is not null)
            {
                matches[additive.Id] = match;
            }
        }

        var byENumber = BuildENumberLookup(candidateList);
        if (byENumber.Count > 0)
        {
            // Scans the original text, not the normalized haystack, since the regex itself
            // already tolerates the space/hyphen/none variants between "E" and the digits.
            foreach (Match capture in ENumberPattern.Matches(text))
            {
                var normalized = NormalizeENumber("E" + capture.Groups[1].Value);
                if (byENumber.TryGetValue(normalized, out var additive) && !matches.ContainsKey(additive.Id))
                {
                    matches[additive.Id] = new AdditiveMatch(additive, capture.Value.Trim(), MatchKind.ENumber);
                }
            }
        }

        var orderedMatches = matches.Values.OrderBy(m => m.Additive.Id).ToList();
        var worst = PickWorst(orderedMatches, includeFunctionalNecessity);
        var overallRiskBand = worst is null
            ? null
            : worst.Additive.Grading!.ComputeEffectiveScore(includeFunctionalNecessity)?.RiskBand;

        return new AdditiveDetectionResult(orderedMatches, overallRiskBand, worst);
    }

    /// <summary>
    /// Picks the highest-severity graded match among the given matches, using RiskBand's
    /// ascending declaration order (Safe..Avoid) as severity rank, then final score, then
    /// additive id as a stable tiebreaker. Returns null if none of the matches are graded.
    /// Never picks from ungraded matches — no verdict without evidence. Shared by both the
    /// live analyze path and the scan-history read path so risk-band ranking logic lives in
    /// one place. Ranks by the effective score (see <see cref="SafetyGrading.ComputeEffectiveScore"/>)
    /// so a user's functional-necessity setting is honored, not just the stored grade.
    /// </summary>
    public static AdditiveMatch? PickWorst(IEnumerable<AdditiveMatch> matches, bool includeFunctionalNecessity = true) => matches
        .Select(m => (Match: m, Effective: m.Additive.Grading?.ComputeEffectiveScore(includeFunctionalNecessity)))
        .Where(x => x.Effective is not null)
        .OrderByDescending(x => (int)x.Effective!.Value.RiskBand)
        .ThenByDescending(x => x.Effective!.Value.FinalScore)
        .ThenBy(x => x.Match.Additive.Id)
        .Select(x => x.Match)
        .FirstOrDefault();

    private static AdditiveMatch? FindNameOrSynonymMatch(Additive additive, string haystack)
    {
        if (ContainsTerm(haystack, additive.Name))
        {
            return new AdditiveMatch(additive, additive.Name, MatchKind.Name);
        }

        if (!string.Equals(additive.Name, additive.NameVerbatim, StringComparison.OrdinalIgnoreCase)
            && ContainsTerm(haystack, additive.NameVerbatim))
        {
            return new AdditiveMatch(additive, additive.NameVerbatim, MatchKind.Name);
        }

        foreach (var synonym in additive.CommonNamesAndSynonyms)
        {
            if (ContainsTerm(haystack, synonym))
            {
                return new AdditiveMatch(additive, synonym, MatchKind.Synonym);
            }
        }

        return null;
    }

    private static bool ContainsTerm(string haystack, string term)
    {
        var normalizedTerm = Normalize(term);
        return normalizedTerm.Length > 0 && haystack.Contains($" {normalizedTerm} ", StringComparison.Ordinal);
    }

    private static Dictionary<string, Additive> BuildENumberLookup(IReadOnlyList<Additive> candidates)
    {
        var lookup = new Dictionary<string, Additive>(StringComparer.Ordinal);
        foreach (var additive in candidates)
        {
            if (additive.ENumber is { Length: > 0 } eNumber)
            {
                lookup[NormalizeENumber(eNumber)] = additive;
            }
        }

        return lookup;
    }

    /// <summary>
    /// Collapses any written form ("E150d", "E 150 D", "150d") to one canonical key so dataset
    /// values and regex captures line up regardless of spacing, casing, or a missing "E".
    /// </summary>
    /// <param name="eNumber"></param>
    /// <returns></returns>
    private static string NormalizeENumber(string eNumber)
    {
        var digitsAndSuffix = eNumber.Trim().TrimStart('E', 'e').Replace(" ", "").Replace("-", "");
        if (digitsAndSuffix.Length == 0)
        {
            return "";
        }

        // Walk back from the end to split off the optional trailing suffix letter (e.g. the
        // "d" in "150d") from the digits.
        var suffixStart = digitsAndSuffix.Length;
        while (suffixStart > 0 && char.IsLetter(digitsAndSuffix[suffixStart - 1]))
        {
            suffixStart--;
        }

        var digits = digitsAndSuffix[..suffixStart];
        var suffix = digitsAndSuffix[suffixStart..].ToLowerInvariant();
        return $"E{digits}{suffix}";
    }

    /// <summary>
    /// Lowercases and collapses everything but letters/digits/hyphens to single spaces, so
    /// hyphenated names like "4-Hexylresorcinol" stay one token while punctuation noise from
    /// OCR doesn't block a match.
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    private static string Normalize(string text)
    {
        var builder = new StringBuilder(text.Length);
        var lastWasSpace = false;

        foreach (var c in text.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c) || c == '-')
            {
                builder.Append(c);
                lastWasSpace = false;
            }
            else if (!lastWasSpace)
            {
                builder.Append(' ');
                lastWasSpace = true;
            }
        }

        return builder.ToString().Trim();
    }
}
