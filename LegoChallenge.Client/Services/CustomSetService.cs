using LegoChallenge.Client.Models;

namespace LegoChallenge.Client.Services;

public static class CustomSetService
{
    // Above this threshold C(n, ceil(n/2)) grows too large for brute force;
    // greedy-add is used instead (not guaranteed optimal but always correct).
    private const int BruteForceMaxN = 20;

    /// <summary>
    /// Finds the largest spec (most distinct piece types, total quantity as tiebreak)
    /// such that at least ceil(n/2) users can build it. Every user in the chosen
    /// qualifying set can build the result by construction — quantities are capped at
    /// each member's minimum owned.
    /// </summary>
    public static List<CustomSetPiece> BuildOptimalSet(List<User> users)
    {
        var n = users.Count;
        if (n == 0) return [];

        var qualifyingCount = (int)Math.Ceiling(n / 2.0);

        return n <= BruteForceMaxN
            ? BruteForceOptimalSet(users, qualifyingCount)
            : GreedyAddOptimalSet(users, qualifyingCount);
    }

    public static bool CanBuildSpec(List<CustomSetPiece> spec, User user)
    {
        var inventory = user.Collection.ToDictionary(
            p => p.PieceId,
            p => p.Variants.ToDictionary(v => v.Color, v => v.Count));

        return spec.All(p =>
            inventory.TryGetValue(p.DesignId, out var variants) &&
            variants.TryGetValue(p.ColorCode.ToString(), out var count) &&
            count >= p.Quantity);
    }

    // Enumerate all C(n, qualifyingCount) subsets, pick the one whose intersection
    // spec is the largest.
    private static List<CustomSetPiece> BruteForceOptimalSet(List<User> users, int qualifyingCount)
    {
        var best = new List<CustomSetPiece>();
        foreach (var indices in Subsets(users.Count, qualifyingCount))
        {
            var spec = IntersectionSpec(indices.Select(i => users[i]));
            if (IsBetter(spec, best)) best = spec;
        }
        return best;
    }

    // Greedily add users one at a time, each time picking the candidate whose
    // addition maximises the intersection spec. O(qualifyingCount * n * pieces).
    private static List<CustomSetPiece> GreedyAddOptimalSet(List<User> users, int qualifyingCount)
    {
        var remaining = Enumerable.Range(0, users.Count).ToList();
        var chosen    = new List<int>();

        for (var i = 0; i < qualifyingCount; i++)
        {
            var bestIdx  = -1;
            var bestSpec = new List<CustomSetPiece>();

            foreach (var idx in remaining)
            {
                var spec = IntersectionSpec(chosen.Append(idx).Select(j => users[j]));
                if (IsBetter(spec, bestSpec)) { bestSpec = spec; bestIdx = idx; }
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
                .Select(v => (Key: (s.PieceId, v.Color), v.Count)))
            .ToDictionary(x => x.Key, x => x.Count);

        foreach (var user in users.Skip(1))
        {
            var inv = user.Collection
                .SelectMany(s => s.Variants.Select(v => (Key: (s.PieceId, v.Color), v.Count)))
                .ToDictionary(x => x.Key, x => x.Count);

            foreach (var key in minQty.Keys.ToList())
            {
                if (!inv.TryGetValue(key, out var cnt) || cnt == 0)
                    minQty.Remove(key);
                else
                    minQty[key] = Math.Min(minQty[key], cnt);
            }
        }

        var result = new List<CustomSetPiece>();
        foreach (var (key, qty) in minQty)
            if (int.TryParse(key.Color, out var colorCode))
                result.Add(new CustomSetPiece(key.PieceId, colorCode, qty));
        return result;
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
