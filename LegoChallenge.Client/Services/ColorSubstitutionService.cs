using LegoChallenge.Client.Models;

namespace LegoChallenge.Client.Services;

public static class ColorSubstitutionService
{
    /// <summary>
    /// Finds a valid colour-substitution assignment using bipartite matching over ALL
    /// candidate colours (including other set-colours). After matching, a co-requirement
    /// loop ensures every set-colour used as a substitute also has its own group matched
    /// (i.e. it is itself being substituted away). Only succeeds when every short group
    /// is covered and all co-requirements are satisfied — no partial results.
    /// </summary>
    public static SubstitutedBuildResult Analyze(LegoSet set, User user)
    {
        // Int-keyed inventory avoids repeated string/int conversions throughout.
        var inventory = user.Collection.ToDictionary(
            p => p.PieceId,
            p => p.Variants
                .Where(v => int.TryParse(v.Color, out _))
                .ToDictionary(v => int.Parse(v.Color), v => v.Count));

        var setColors = set.Pieces.Select(p => p.Part.Material).ToHashSet();

        var allGroups = set.Pieces
            .GroupBy(p => p.Part.Material)
            .Select(g => (Color: g.Key, Pieces: g.ToList()))
            .ToList();

        var shortGroups = allGroups
            .Where(g => !g.Pieces.All(p => Own(inventory, p.Part.DesignId, g.Color) >= p.Quantity))
            .ToList();

        if (shortGroups.Count == 0)
            return new SubstitutedBuildResult(true, [], []);

        var shortColors     = shortGroups.Select(g => g.Color).ToHashSet();
        var fulfilledGroups = allGroups.Where(g => !shortColors.Contains(g.Color)).ToList();

        var adjacency = allGroups.ToDictionary(
            g => g.Color,
            g => AllCandidates(g.Color, g.Pieces, inventory));

        var origToSub = new Dictionary<int, int>();
        var subToOrig = new Dictionary<int, int>();

        foreach (var (color, _) in shortGroups)
            Augment(color, adjacency, origToSub, subToOrig, new HashSet<int>());

        foreach (var (color, _) in fulfilledGroups)
            Augment(color, adjacency, origToSub, subToOrig, new HashSet<int>());

        bool changed;
        do
        {
            changed = false;
            foreach (var s in subToOrig.Keys.Where(s => setColors.Contains(s) && !origToSub.ContainsKey(s)).ToList())
            {
                if (Augment(s, adjacency, origToSub, subToOrig, new HashSet<int>()))
                    changed = true;
            }
        } while (changed);

        bool allShortMatched = shortGroups.All(g => origToSub.ContainsKey(g.Color));
        bool coReqsMet       = subToOrig.Keys.Where(s => setColors.Contains(s)).All(s => origToSub.ContainsKey(s));

        if (allShortMatched && coReqsMet)
        {
            var subs = origToSub.Select(kv => new SubstitutionInfo(kv.Key, kv.Value)).ToList();
            return new SubstitutedBuildResult(true, [], subs);
        }

        var missing = shortGroups
            .SelectMany(g => g.Pieces
                .Select(p => (Piece: p, Have: Own(inventory, p.Part.DesignId, g.Color)))
                .Where(x => x.Have < x.Piece.Quantity)
                .Select(x => new MissingPiece(x.Piece.Part.DesignId, g.Color, x.Piece.Quantity, x.Have)))
            .ToList();

        return new SubstitutedBuildResult(false, missing, []);
    }

    private static List<int> AllCandidates(
        int originalColor,
        List<SetPiece> pieces,
        Dictionary<string, Dictionary<int, int>> inventory) =>
        pieces
            .Where(p => inventory.ContainsKey(p.Part.DesignId))
            .SelectMany(p => inventory[p.Part.DesignId].Keys)
            .Where(c => c != originalColor)
            .Distinct()
            .Where(c => pieces.All(p => Own(inventory, p.Part.DesignId, c) >= p.Quantity))
            .ToList();

    private static bool Augment(
        int color,
        Dictionary<int, List<int>> adjacency,
        Dictionary<int, int> origToSub,
        Dictionary<int, int> subToOrig,
        HashSet<int> visited)
    {
        foreach (var sub in adjacency[color])
        {
            if (!visited.Add(sub)) continue;

            if (!subToOrig.TryGetValue(sub, out var incumbent) ||
                Augment(incumbent, adjacency, origToSub, subToOrig, visited))
            {
                origToSub[color] = sub;
                subToOrig[sub]   = color;
                return true;
            }
        }
        return false;
    }

    private static int Own(
        Dictionary<string, Dictionary<int, int>> inventory,
        string designId, int color) =>
        inventory.TryGetValue(designId, out var v) && v.TryGetValue(color, out var n) ? n : 0;
}
