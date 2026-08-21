using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Data;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

public class CollectionProfileService(AppDbContext context) : ICollectionProfileService
{
    private static readonly TimeSpan UnderusedAge = TimeSpan.FromDays(180);
    private static readonly TimeSpan NewWatchGracePeriod = TimeSpan.FromDays(30);
    private static readonly TimeSpan StaleResaleAge = TimeSpan.FromDays(30);

    public async Task<CollectionProfileDto> GetProfileAsync(int userId, CancellationToken ct = default)
    {
        var active = await context.Watches
            .AsNoTracking()
            .Where(w => w.UserId == userId && !w.IsWishList && w.Disposition == null)
            .OrderBy(w => w.Id)
            .ToListAsync(ct);
        var wishlist = await context.Watches
            .AsNoTracking()
            .Where(w => w.UserId == userId && w.IsWishList)
            .OrderBy(w => w.Id)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        var profile = new CollectionProfileDto
        {
            ActiveWatchCount = active.Count,
            WishlistWatchCount = wishlist.Count,
            DataCompletenessPercent = CalculateCompleteness(active),
            UnderusedWatchIds = active
                .Where(w => now - w.CreatedAt >= NewWatchGracePeriod
                    && (w.TimesWorn == 0 || w.LastWornDate is null || now - w.LastWornDate >= UnderusedAge))
                .Select(w => w.Id)
                .ToList(),
            StaleResaleValueWatchIds = active
                .Where(w => w.ResaleValueUpdatedAt is null || now - w.ResaleValueUpdatedAt >= StaleResaleAge)
                .Select(w => w.Id)
                .ToList(),
            WishlistOverlaps = FindWishlistOverlaps(active, wishlist)
        };

        profile.Coverage =
        [
            Coverage("Movement", active, w => w.MovementType.ToString()),
            Coverage("Case size", active, w => CaseSizeBand(w.CaseSizeMm)),
            Coverage("Dial color", active, w => Normalize(w.DialColor)),
            Coverage("Band type", active, w => Normalize(w.BandType)),
            Coverage("Purchase price", active, w => PriceBand(w.PurchasePrice))
        ];
        profile.Gaps = FindGaps(active, profile.Coverage);
        profile.Redundancies = FindRedundancies(active);
        profile.DataQuality = FindDataQuality(active);

        return profile;
    }

    public CandidateFitScoreDto ScoreCandidate(
        CollectionProfileDto profile,
        CollectionCandidateProfile candidate,
        decimal? budget = null)
    {
        var reasons = new List<string>();
        var knownSignals = 0;
        var fitPoints = 50;

        if (candidate.MovementType is MovementType movement)
        {
            knownSignals++;
            fitPoints += AddNovelty(
                profile,
                "Movement",
                movement.ToString(),
                15,
                reasons,
                $"Adds a {movement} movement.",
                $"Repeats the collection's {movement} movement coverage.");
        }

        if (candidate.CaseSizeMm is double caseSize)
        {
            knownSignals++;
            var band = CaseSizeBand(caseSize)!;
            fitPoints += AddNovelty(
                profile,
                "Case size",
                band,
                10,
                reasons,
                $"Adds the {band.ToLowerInvariant()} case-size range.",
                $"Falls in the existing {band.ToLowerInvariant()} case-size range.");
        }

        if (Normalize(candidate.DialColor) is { } dial)
        {
            knownSignals++;
            fitPoints += AddNovelty(
                profile,
                "Dial color",
                dial,
                10,
                reasons,
                $"Adds {dial} dial coverage.",
                $"Repeats an existing {dial} dial.");
        }

        if (Normalize(candidate.BandType) is { } bandType)
        {
            knownSignals++;
            fitPoints += AddNovelty(
                profile,
                "Band type",
                bandType,
                10,
                reasons,
                $"Adds {bandType} band coverage.",
                $"Repeats an existing {bandType} band type.");
        }

        var collectionFit = Math.Clamp(fitPoints, 0, 100);
        int? budgetFit = null;
        if (budget is > 0 && candidate.Price is decimal price)
        {
            budgetFit = price <= budget
                ? 100
                : Math.Clamp((int)Math.Round(100m - ((price - budget.Value) / budget.Value * 200m)), 0, 100);
            reasons.Add(price <= budget
                ? $"The candidate is within the {budget.Value:C0} budget."
                : $"The candidate is {price - budget.Value:C0} over the {budget.Value:C0} budget.");
        }

        var confidence = knownSignals * 25;
        var total = budgetFit is int budgetScore
            ? (int)Math.Round(collectionFit * 0.6 + budgetScore * 0.3 + confidence * 0.1)
            : (int)Math.Round(collectionFit * 0.85 + confidence * 0.15);

        if (knownSignals < 2)
            reasons.Add("The fit score has low confidence because the candidate has limited structured metadata.");

        return new CandidateFitScoreDto
        {
            TotalScore = Math.Clamp(total, 0, 100),
            CollectionFitScore = collectionFit,
            BudgetFitScore = budgetFit,
            EvidenceConfidencePercent = confidence,
            Reasons = reasons
        };
    }

    private static int AddNovelty(
        CollectionProfileDto profile,
        string dimension,
        string value,
        int points,
        List<string> reasons,
        string novelReason,
        string repeatedReason)
    {
        var exists = profile.Coverage
            .FirstOrDefault(c => c.Dimension == dimension)?
            .Values.Any(v => v.Value.Equals(value, StringComparison.OrdinalIgnoreCase)) == true;
        reasons.Add(exists ? repeatedReason : novelReason);
        return exists ? 0 : points;
    }

    private static CollectionCoverageDto Coverage(
        string dimension,
        IEnumerable<Watch> watches,
        Func<Watch, string?> selector)
    {
        return new CollectionCoverageDto
        {
            Dimension = dimension,
            Values = watches
                .Select(w => new { Watch = w, Value = selector(w) })
                .Where(x => x.Value is not null)
                .GroupBy(x => x.Value!, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .Select(g => new CollectionCoverageValueDto
                {
                    Value = g.Key,
                    Count = g.Count(),
                    WatchIds = g.Select(x => x.Watch.Id).ToList()
                })
                .ToList()
        };
    }

    private static int CalculateCompleteness(IReadOnlyCollection<Watch> watches)
    {
        if (watches.Count == 0) return 0;

        var populated = watches.Sum(w =>
            Present(w.CaseSizeMm)
            + Present(w.DialColor)
            + Present(w.BandType)
            + Present(w.PurchasePrice)
            + Present(w.WaterResistance)
            + Present(w.CurrentResaleValue));
        return (int)Math.Round(populated * 100d / (watches.Count * 6));
    }

    private static List<CollectionInsightDto> FindGaps(
        IReadOnlyCollection<Watch> watches,
        IReadOnlyCollection<CollectionCoverageDto> coverage)
    {
        if (watches.Count < 3) return [];

        var gaps = new List<CollectionInsightDto>();
        AddLowVarietyGap(gaps, coverage, watches.Count, "Movement", "Movement variety");
        AddLowVarietyGap(gaps, coverage, watches.Count, "Case size", "Case-size variety");
        AddLowVarietyGap(gaps, coverage, watches.Count, "Dial color", "Dial-color variety");
        AddLowVarietyGap(gaps, coverage, watches.Count, "Band type", "Strap and bracelet variety");
        return gaps;
    }

    private static void AddLowVarietyGap(
        List<CollectionInsightDto> gaps,
        IReadOnlyCollection<CollectionCoverageDto> coverage,
        int watchCount,
        string dimension,
        string summary)
    {
        var dimensionCoverage = coverage.First(c => c.Dimension == dimension);
        var knownCount = dimensionCoverage.Values.Sum(v => v.Count);
        if (knownCount < Math.Ceiling(watchCount * 0.67) || dimensionCoverage.Values.Count != 1) return;

        var onlyValue = dimensionCoverage.Values[0];
        gaps.Add(new CollectionInsightDto
        {
            Summary = summary,
            Reason = $"{knownCount} of {watchCount} watches with known {dimension.ToLowerInvariant()} data share only \"{onlyValue.Value}\" coverage.",
            Confidence = knownCount == watchCount
                ? CollectionInsightConfidence.High
                : CollectionInsightConfidence.Medium,
            WatchIds = onlyValue.WatchIds,
            EvidenceFields = [dimension]
        });
    }

    private static List<CollectionInsightDto> FindRedundancies(IReadOnlyCollection<Watch> watches)
    {
        var redundancies = watches
            .GroupBy(w => $"{Normalize(w.Brand)}|{Normalize(w.Model)}")
            .Where(g => g.Count() > 1)
            .Select(g => new CollectionInsightDto
            {
                Summary = "Duplicate brand and model",
                Reason = $"{g.Count()} active watches have the same normalized brand and model.",
                Confidence = CollectionInsightConfidence.High,
                WatchIds = g.Select(w => w.Id).ToList(),
                EvidenceFields = ["Brand", "Model"]
            })
            .ToList();

        var traitClusters = watches
            .Where(w => w.CaseSizeMm is not null && Normalize(w.DialColor) is not null)
            .GroupBy(w => $"{w.MovementType}|{CaseSizeBand(w.CaseSizeMm)}|{Normalize(w.DialColor)}")
            .Where(g => g.Count() >= 3);
        redundancies.AddRange(traitClusters.Select(g => new CollectionInsightDto
        {
            Summary = "Repeated movement, size, and dial profile",
            Reason = $"{g.Count()} active watches share the same movement, case-size range, and dial color.",
            Confidence = CollectionInsightConfidence.Medium,
            WatchIds = g.Select(w => w.Id).ToList(),
            EvidenceFields = ["Movement", "Case size", "Dial color"]
        }));

        return redundancies;
    }

    private static List<CollectionInsightDto> FindDataQuality(IReadOnlyCollection<Watch> watches)
    {
        if (watches.Count == 0)
        {
            return
            [
                new CollectionInsightDto
                {
                    Summary = "No active collection data",
                    Reason = "Add at least one active watch before evaluating collection coverage.",
                    Confidence = CollectionInsightConfidence.High
                }
            ];
        }

        var fields = new (string Name, Func<Watch, bool> IsMissing)[]
        {
            ("Case size", w => w.CaseSizeMm is null),
            ("Dial color", w => Normalize(w.DialColor) is null),
            ("Band type", w => Normalize(w.BandType) is null),
            ("Purchase price", w => w.PurchasePrice is null),
            ("Water resistance", w => Normalize(w.WaterResistance) is null),
            ("Current resale value", w => w.CurrentResaleValue is null)
        };

        return fields
            .Select(field => new
            {
                field.Name,
                WatchIds = watches.Where(field.IsMissing).Select(w => w.Id).ToList()
            })
            .Where(x => x.WatchIds.Count > 0)
            .Select(x => new CollectionInsightDto
            {
                Summary = $"Missing {x.Name.ToLowerInvariant()} data",
                Reason = $"{x.WatchIds.Count} of {watches.Count} active watches are missing {x.Name.ToLowerInvariant()} data; conclusions using that field have lower confidence.",
                Confidence = CollectionInsightConfidence.High,
                WatchIds = x.WatchIds,
                EvidenceFields = [x.Name]
            })
            .ToList();
    }

    private static List<WishlistOverlapDto> FindWishlistOverlaps(
        IReadOnlyCollection<Watch> active,
        IReadOnlyCollection<Watch> wishlist)
    {
        return wishlist
            .Select(w => new
            {
                Watch = w,
                Matches = active
                    .Where(a => Normalize(a.Brand) == Normalize(w.Brand) && Normalize(a.Model) == Normalize(w.Model))
                    .Select(a => a.Id)
                    .ToList()
            })
            .Where(x => x.Matches.Count > 0)
            .Select(x => new WishlistOverlapDto
            {
                WishlistWatchId = x.Watch.Id,
                CollectionWatchIds = x.Matches,
                Reason = "The wishlist item has the same normalized brand and model as an active watch."
            })
            .ToList();
    }

    private static string? CaseSizeBand(double? millimeters) => millimeters switch
    {
        null => null,
        <= 36 => "Compact (36 mm or less)",
        <= 40 => "Medium (over 36 to 40 mm)",
        _ => "Large (over 40 mm)"
    };

    private static string? PriceBand(decimal? price) => price switch
    {
        null => null,
        < 500 => "Under $500",
        < 2000 => "$500-$1,999",
        < 5000 => "$2,000-$4,999",
        _ => "$5,000 and above"
    };

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();

    private static int Present(object? value) => value is null ? 0 : 1;
}
