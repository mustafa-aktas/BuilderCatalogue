using LegoChallenge.Client.Models;

namespace LegoChallenge.Client.Services;

public static class BuildAnalysisService
{
    public static BuildResult Analyze(SetSummary summary, LegoSet set, User user)
    {
        var missing = GetMissing(set, user);
        return new BuildResult(summary, missing.Count == 0, missing);
    }

    internal static List<MissingPiece> GetMissing(LegoSet set, User user)
    {
        var inventory = user.Collection.ToDictionary(
            p => p.PieceId,
            p => p.Variants.ToDictionary(v => v.Color, v => v.Count));

        var missing = new List<MissingPiece>();

        foreach (var piece in set.Pieces)
        {
            var designId = piece.Part.DesignId;
            var colorKey = piece.Part.Material.ToString();
            var required = piece.Quantity;

            var have = inventory.TryGetValue(designId, out var variants)
                    && variants.TryGetValue(colorKey, out var count) ? count : 0;

            if (have < required)
                missing.Add(new MissingPiece(designId, piece.Part.Material, required, have));
        }

        return missing;
    }
}
