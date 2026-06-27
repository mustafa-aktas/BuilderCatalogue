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

        // Pre-build per-candidate inventories once rather than inside the loop.
        var pool = candidates
            .Select(c => (c.Summary, Inv: BuildInventory(c.Detail)))
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

    private static Dictionary<(string DesignId, int ColorCode), int> BuildInventory(User user)
    {
        var result = new Dictionary<(string, int), int>();
        foreach (var stock in user.Collection)
            foreach (var v in stock.Variants)
                if (int.TryParse(v.Color, out var colorCode))
                {
                    var key = (stock.PieceId, colorCode);
                    result[key] = result.GetValueOrDefault(key) + v.Count;
                }
        return result;
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
