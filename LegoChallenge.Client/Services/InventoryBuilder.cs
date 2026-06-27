using LegoChallenge.Client.Models;

namespace LegoChallenge.Client.Services;

public static class InventoryBuilder
{
    /// <summary>Flat (designId, colorCode) → count. For deficit/coverage calculations.</summary>
    public static Dictionary<(string DesignId, int ColorCode), int> BuildFlat(User user)
    {
        var result = new Dictionary<(string, int), int>();
        foreach (var stock in user.Collection)
            foreach (var v in stock.Variants)
            {
                var key = (stock.PieceId, v.ColorCode);
                result[key] = result.GetValueOrDefault(key) + v.Count;
            }
        return result;
    }

    /// <summary>Nested designId → colorCode → count. For per-piece lookups.</summary>
    public static Dictionary<string, Dictionary<int, int>> BuildNested(User user) =>
        user.Collection.ToDictionary(
            p => p.PieceId,
            p => p.Variants.ToDictionary(v => v.ColorCode, v => v.Count));
}
