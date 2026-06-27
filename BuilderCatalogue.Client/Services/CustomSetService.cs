using BuilderCatalogue.Client.Models;

namespace BuilderCatalogue.Client.Services;

public static class CustomSetService
{
    // Above this threshold the qualifying-user combinations grow too large for brute force;
    // greedy-add is used instead (not guaranteed optimal but always correct).
    private const int BruteForceMaxN = 20;

    /// <summary>
    /// Finds the largest spec (most distinct piece types, total quantity as tiebreak)
    /// such that at least the requested percentage of users can build it. Required
    /// users are always included in the qualifying group. Quantities are capped at
    /// each qualifying member's minimum owned.
    /// </summary>
    public static List<CustomSetPiece> BuildOptimalSet(
        List<User> users,
        int targetPercentage = 50,
        IReadOnlyCollection<string>? requiredUserIds = null)
    {
        if (targetPercentage is < 1 or > 100)
            throw new ArgumentOutOfRangeException(
                nameof(targetPercentage),
                targetPercentage,
                "Target percentage must be between 1 and 100.");

        var requiredIds = requiredUserIds?.ToHashSet() ?? [];
        var availableIds = users.Select(u => u.Id).ToHashSet();
        var unknownIds = requiredIds.Except(availableIds).ToList();
        if (unknownIds.Count > 0)
            throw new ArgumentException(
                $"Required users were not found: {string.Join(", ", unknownIds)}.",
                nameof(requiredUserIds));

        var n = users.Count;
        if (n == 0) return [];

        var requiredIndices = Enumerable.Range(0, n)
            .Where(i => requiredIds.Contains(users[i].Id))
            .ToList();
        var qualifyingCount = Math.Max(
            (int)Math.Ceiling(n * targetPercentage / 100.0),
            requiredIndices.Count);

        return n <= BruteForceMaxN
            ? BruteForceOptimalSet(users, qualifyingCount, requiredIndices)
            : GreedyAddOptimalSet(users, qualifyingCount, requiredIndices);
    }

    public static bool CanBuildSpec(List<CustomSetPiece> spec, User user)
    {
        var inventory = InventoryBuilder.BuildNested(user);
        return spec.All(p =>
            inventory.TryGetValue(p.DesignId, out var variants) &&
            variants.TryGetValue(p.ColorCode, out var count) &&
            count >= p.Quantity);
    }

    // Enumerate all C(n, qualifyingCount) subsets, pick the one whose intersection
    // spec is the largest.
    private static List<CustomSetPiece> BruteForceOptimalSet(
        List<User> users,
        int qualifyingCount,
        List<int> requiredIndices)
    {
        var required = requiredIndices.ToHashSet();
        var optionalIndices = Enumerable.Range(0, users.Count)
            .Where(i => !required.Contains(i))
            .ToList();
        var optionalCount = qualifyingCount - requiredIndices.Count;
        var best = new List<CustomSetPiece>();
        foreach (var optionalSubset in Subsets(optionalIndices.Count, optionalCount))
        {
            var indices = requiredIndices.Concat(optionalSubset.Select(i => optionalIndices[i]));
            var spec = IntersectionSpec(indices.Select(i => users[i]));
            if (IsBetter(spec, best)) best = spec;
        }
        return best;
    }

    // Greedily add users one at a time, each time picking the candidate whose
    // addition maximises the intersection spec. O(qualifyingCount * n * pieces).
    private static List<CustomSetPiece> GreedyAddOptimalSet(
        List<User> users,
        int qualifyingCount,
        List<int> requiredIndices)
    {
        var chosen = requiredIndices.ToList();
        var required = requiredIndices.ToHashSet();
        var remaining = Enumerable.Range(0, users.Count)
            .Where(i => !required.Contains(i))
            .ToList();

        while (chosen.Count < qualifyingCount)
        {
            var bestIdx  = -1;
            var bestSpec = new List<CustomSetPiece>();

            foreach (var idx in remaining)
            {
                var spec = IntersectionSpec(chosen.Append(idx).Select(j => users[j]));
                if (bestIdx < 0 || IsBetter(spec, bestSpec))
                {
                    bestSpec = spec;
                    bestIdx = idx;
                }
            }

            if (bestIdx < 0) break;
            chosen.Add(bestIdx);
            remaining.Remove(bestIdx);
        }

        return IntersectionSpec(chosen.Select(i => users[i]));
    }

    // Intersection inventory of a group of users: only pieces ALL of them own,
    // at quantities no higher than the minimum any of them holds.
    internal static List<CustomSetPiece> IntersectionSpec(IEnumerable<User> group)
    {
        var users = group.ToList();
        if (users.Count == 0) return [];

        var minQty = users[0].Collection
            .SelectMany(s => s.Variants
                .Where(v => v.Count > 0)
                .Select(v => (Key: (s.PieceId, v.ColorCode), v.Count)))
            .ToDictionary(x => x.Key, x => x.Count);

        foreach (var user in users.Skip(1))
        {
            var inv = user.Collection
                .SelectMany(s => s.Variants.Select(v => (Key: (s.PieceId, v.ColorCode), v.Count)))
                .ToDictionary(x => x.Key, x => x.Count);

            foreach (var key in minQty.Keys.ToList())
            {
                if (!inv.TryGetValue(key, out var cnt) || cnt == 0)
                    minQty.Remove(key);
                else
                    minQty[key] = Math.Min(minQty[key], cnt);
            }
        }

        return minQty
            .Select(kv => new CustomSetPiece(kv.Key.PieceId, kv.Key.ColorCode, kv.Value))
            .ToList();
    }

    // More distinct types = better; equal types → higher total quantity wins.
    private static bool IsBetter(List<CustomSetPiece> candidate, List<CustomSetPiece> current) =>
        candidate.Count > current.Count ||
        (candidate.Count == current.Count &&
         candidate.Sum(p => p.Quantity) > current.Sum(p => p.Quantity));

    // Yields all size-k index subsets of [0..n) in lexicographic order.
    private static IEnumerable<List<int>> Subsets(int n, int k)
    {
        var indices = new int[k];
        for (var i = 0; i < k; i++) indices[i] = i;

        while (true)
        {
            yield return [.. indices];

            var pos = k - 1;
            while (pos >= 0 && indices[pos] == n - k + pos) pos--;
            if (pos < 0) yield break;

            indices[pos]++;
            for (var i = pos + 1; i < k; i++) indices[i] = indices[i - 1] + 1;
        }
    }
}
