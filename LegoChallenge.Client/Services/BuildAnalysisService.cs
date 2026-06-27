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
        var inventory = InventoryBuilder.BuildNested(user);
        var missing   = new List<MissingPiece>();

        foreach (var piece in set.Pieces)
        {
            var required = piece.Quantity;
            var have     = inventory.TryGetValue(piece.Part.DesignId, out var variants)
                        && variants.TryGetValue(piece.Part.ColorCode, out var count) ? count : 0;

            if (have < required)
                missing.Add(new MissingPiece(piece.Part.DesignId, piece.Part.ColorCode, required, have));
        }

        return missing;
    }
}
