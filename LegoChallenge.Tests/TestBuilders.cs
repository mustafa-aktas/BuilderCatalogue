using LegoChallenge.Client.Models;

namespace LegoChallenge.Tests;

internal static class Build
{
    internal static SetPiece Piece(string designId, int color, int qty) =>
        new(new PartInfo(designId, color, "Part"), qty);

    internal static LegoSet Set(params SetPiece[] pieces) =>
        new("set-1", "Test Set", "1-1", pieces.Sum(p => p.Quantity), [.. pieces]);

    internal static User UserWith(params (string designId, int color, int count)[] owned)
    {
        var stocks = owned
            .GroupBy(x => x.designId)
            .Select(g => new PieceStock(
                g.Key,
                g.Select(x => new PieceVariant(x.color.ToString(), x.count)).ToList()))
            .ToList();
        return new User("u1", "tester", "NL", stocks.Sum(s => s.Variants.Sum(v => v.Count)), stocks);
    }

    internal static SetSummary Summary(LegoSet set) =>
        new(set.Id, set.Name, set.SetNumber, set.TotalPieces);

    internal static UserSummary SummaryOf(User u) =>
        new(u.Id, u.Username, u.Location, u.BrickCount);
}
