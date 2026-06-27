using System.Numerics;
using LegoChallenge.Client.Models;

namespace LegoChallenge.Client.Services;

public static class CollaborationService
{
    /// <summary>
    /// Greedy set-cover: find the minimum number of collaborators whose combined
    /// inventory fills every piece the primary user is missing for the given set.
    /// </summary>
    public static CollaborationResult FindMinimumCollaborators(
        LegoSet set,
        User primary,
        IEnumerable<(UserSummary Summary, User Detail)> candidates)
    {
        var deficit = BuildAnalysisService.GetMissing(set, primary)
            .ToDictionary(m => (m.DesignId, m.ColorCode), m => m.ShortBy);

        if (deficit.Count == 0)
            return new CollaborationResult(true, [], []);

        var pool = candidates
            .Select(c => (c.Summary, Inv: InventoryBuilder.BuildFlat(c.Detail)))
            .ToList();

        var collaborators = new List<CollaboratorContribution>();

        while (deficit.Count > 0 && pool.Count > 0)
        {
            var bestIdx      = -1;
            var bestCoverage = new Dictionary<(string DesignId, int ColorCode), int>();
            var bestScore    = 0;

            for (var i = 0; i < pool.Count; i++)
            {
                var cov   = GetCoverage(pool[i].Inv, deficit);
                var score = cov.Values.Sum();
                if (score > bestScore) { bestScore = score; bestCoverage = cov; bestIdx = i; }
            }

            if (bestIdx < 0) break;

            var (bestSummary, _) = pool[bestIdx];
            pool.RemoveAt(bestIdx);

            var contributions = bestCoverage
                .Select(kv => new ContributedPiece(kv.Key.DesignId, kv.Key.ColorCode, kv.Value))
                .ToList();

            foreach (var (key, amount) in bestCoverage)
            {
                deficit[key] -= amount;
                if (deficit[key] <= 0) deficit.Remove(key);
            }

            collaborators.Add(new CollaboratorContribution(bestSummary, contributions));
        }

        var stillMissing = deficit
            .Select(kv => new MissingPiece(kv.Key.DesignId, kv.Key.ColorCode, kv.Value, 0))
            .ToList();

        return new CollaborationResult(deficit.Count == 0, collaborators, stillMissing);
    }

    /// <summary>
    /// Lists every candidate who can contribute at least one missing piece,
    /// sorted by total contribution. Also computes whether all of them together
    /// can fully cover the deficit.
    /// </summary>
    public static CollaborationResult FindAllCollaborators(
        LegoSet set,
        User primary,
        IEnumerable<(UserSummary Summary, User Detail)> candidates)
    {
        var deficit = BuildAnalysisService.GetMissing(set, primary)
            .ToDictionary(m => (m.DesignId, m.ColorCode), m => m.ShortBy);

        if (deficit.Count == 0)
            return new CollaborationResult(true, [], []);

        var collaborators = new List<CollaboratorContribution>();

        foreach (var c in candidates)
        {
            var inv      = InventoryBuilder.BuildFlat(c.Detail);
            var coverage = GetCoverage(inv, deficit);
            if (coverage.Count == 0) continue;

            var contributions = coverage
                .Select(kv => new ContributedPiece(kv.Key.DesignId, kv.Key.ColorCode, kv.Value))
                .ToList();
            collaborators.Add(new CollaboratorContribution(c.Summary, contributions));
        }

        collaborators.Sort((a, b) =>
            b.Pieces.Sum(p => p.Quantity).CompareTo(a.Pieces.Sum(p => p.Quantity)));

        var remaining = deficit.ToDictionary(kv => kv.Key, kv => kv.Value);
        foreach (var collab in collaborators)
            foreach (var piece in collab.Pieces)
            {
                var key = (piece.DesignId, piece.ColorCode);
                if (remaining.TryGetValue(key, out var left))
                {
                    remaining[key] = left - piece.Quantity;
                    if (remaining[key] <= 0) remaining.Remove(key);
                }
            }

        var stillMissing = remaining
            .Select(kv => new MissingPiece(kv.Key.DesignId, kv.Key.ColorCode, kv.Value, 0))
            .ToList();

        return new CollaborationResult(remaining.Count == 0, collaborators, stillMissing);
    }

    /// <summary>
    /// Finds all minimal combinations of users that together fully cover the missing pieces.
    /// A combination is minimal when removing any user from it leaves it incomplete.
    /// Uses bitmask enumeration; capped at 24 candidates.
    /// </summary>
    public static List<CollaborationCombination> FindAllMinimalCombinations(
        LegoSet set,
        User primary,
        IEnumerable<(UserSummary Summary, User Detail)> candidates)
    {
        var deficit = BuildAnalysisService.GetMissing(set, primary)
            .ToDictionary(m => (m.DesignId, m.ColorCode), m => m.ShortBy);

        if (deficit.Count == 0)
            return [new CollaborationCombination([])];

        var pool = candidates
            .Select(c => (c.Summary, Coverage: GetCoverage(InventoryBuilder.BuildFlat(c.Detail), deficit)))
            .Where(x => x.Coverage.Count > 0)
            .Take(24)
            .ToList();

        if (pool.Count == 0) return [];

        int n = pool.Count;
        var covering = new List<int>();

        for (int mask = 1; mask < (1 << n); mask++)
        {
            var remaining = new Dictionary<(string DesignId, int ColorCode), int>(deficit);
            for (int i = 0; i < n; i++)
            {
                if ((mask & (1 << i)) == 0) continue;
                foreach (var (key, amount) in pool[i].Coverage)
                {
                    if (remaining.TryGetValue(key, out var left))
                    {
                        remaining[key] = left - amount;
                        if (remaining[key] <= 0) remaining.Remove(key);
                    }
                }
            }
            if (remaining.Count == 0) covering.Add(mask);
        }

        return covering
            .Where(mask => !covering.Any(sub => sub != mask && (sub & mask) == sub))
            .OrderBy(mask => BitOperations.PopCount((uint)mask))
            .Select(mask =>
            {
                // Allocate deficit sequentially so contributors don't double-count shared pieces.
                var remaining = new Dictionary<(string DesignId, int ColorCode), int>(deficit);
                var contributors = new List<CollaboratorContribution>();
                for (int i = 0; i < n; i++)
                {
                    if ((mask & (1 << i)) == 0) continue;
                    var pieces = new List<ContributedPiece>();
                    foreach (var (key, have) in pool[i].Coverage)
                    {
                        if (!remaining.TryGetValue(key, out var stillNeeded)) continue;
                        var actual = Math.Min(have, stillNeeded);
                        if (actual <= 0) continue;
                        pieces.Add(new ContributedPiece(key.DesignId, key.ColorCode, actual));
                        remaining[key] = stillNeeded - actual;
                        if (remaining[key] <= 0) remaining.Remove(key);
                    }
                    if (pieces.Count > 0)
                        contributors.Add(new CollaboratorContribution(pool[i].Summary, pieces));
                }
                return new CollaborationCombination(contributors);
            })
            .ToList();
    }

    private static Dictionary<(string DesignId, int ColorCode), int> GetCoverage(
        Dictionary<(string DesignId, int ColorCode), int> inventory,
        Dictionary<(string DesignId, int ColorCode), int> deficit)
    {
        var coverage = new Dictionary<(string DesignId, int ColorCode), int>();
        foreach (var (key, needed) in deficit)
            if (inventory.TryGetValue(key, out var have) && have > 0)
                coverage[key] = Math.Min(have, needed);
        return coverage;
    }
}
