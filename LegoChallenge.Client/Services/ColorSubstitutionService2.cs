using LegoChallenge.Client.Models;

namespace LegoChallenge.Client.Services;

public static class ColorSubstitutionService2
{
    public static SubstitutedBuildResult Analyze(LegoSet set, User user)
    {
        var inventory = user.Collection.ToDictionary(
            p => p.PieceId,
            p => p.Variants
                .Where(v => int.TryParse(v.Color, out _))
                .ToDictionary(v => int.Parse(v.Color), v => v.Count));

        var colorGroups = set.Pieces
            .GroupBy(p => p.Part.Material)
            .ToDictionary(g => g.Key, g => g.ToList());

        var setColors = colorGroups.Keys.ToHashSet();

        // Pre-compute substitute candidates per color group
        var candidates = colorGroups.ToDictionary(
            kv => kv.Key,
            kv => GetCandidates(kv.Key, kv.Value, inventory));

        var shortGroups = colorGroups
            .Where(kv => !kv.Value.All(p => Own(inventory, p.Part.DesignId, kv.Key) >= p.Quantity))
            .Select(kv => kv.Key)
            .ToHashSet();

        if (shortGroups.Count == 0)
            return new SubstitutedBuildResult(true, [], []);

        // Backtracking over todo list; todo grows when a set-color is claimed as substitute
        // (co-requirement: that color's group must also be substituted)
        var assignment = new Dictionary<int, int>();
        var claimed    = new HashSet<int>();
        var todo       = shortGroups.ToList();

        if (Solve(todo, 0, assignment, claimed, setColors, candidates))
        {
            var subs = assignment.Select(kv => new SubstitutionInfo(kv.Key, kv.Value)).ToList();
            return new SubstitutedBuildResult(true, [], subs);
        }

        var missing = shortGroups
            .SelectMany(color => colorGroups[color]
                .Select(p => (Piece: p, Have: Own(inventory, p.Part.DesignId, color)))
                .Where(x => x.Have < x.Piece.Quantity)
                .Select(x => new MissingPiece(x.Piece.Part.DesignId, color, x.Piece.Quantity, x.Have)))
            .ToList();

        return new SubstitutedBuildResult(false, missing, []);
    }

    private static bool Solve(
        List<int> todo,
        int index,
        Dictionary<int, int> assignment,
        HashSet<int> claimed,
        HashSet<int> setColors,
        Dictionary<int, List<int>> candidates)
    {
        if (index >= todo.Count) return true;

        var color = todo[index];

        foreach (var sub in candidates[color])
        {
            if (claimed.Contains(sub)) continue;

            assignment[color] = sub;
            claimed.Add(sub);

            // If sub is a set-color not yet in todo, it becomes a co-requirement
            bool addedCoReq = setColors.Contains(sub) && !todo.Contains(sub);
            if (addedCoReq) todo.Add(sub);

            if (Solve(todo, index + 1, assignment, claimed, setColors, candidates))
                return true;

            assignment.Remove(color);
            claimed.Remove(sub);
            if (addedCoReq) todo.RemoveAt(todo.Count - 1);
        }

        return false;
    }

    private static List<int> GetCandidates(
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

    private static int Own(
        Dictionary<string, Dictionary<int, int>> inventory,
        string designId, int color) =>
        inventory.TryGetValue(designId, out var v) && v.TryGetValue(color, out var n) ? n : 0;
}
