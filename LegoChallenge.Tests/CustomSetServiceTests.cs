using LegoChallenge.Client.Models;
using LegoChallenge.Client.Services;
using Xunit;

namespace LegoChallenge.Tests;

public class CustomSetServiceTests
{
    // ── BuildOptimalSet ───────────────────────────────────────────────────────

    [Fact]
    public void BuildOptimalSet_EmptyUsers_ReturnsEmpty()
    {
        var result = CustomSetService.BuildOptimalSet([]);
        Assert.Empty(result);
    }

    [Fact]
    public void BuildOptimalSet_SingleUser_ReturnsAllPieces()
    {
        var users = new List<User> { Build.UserWith(("A", 1, 10), ("B", 2, 5)) };

        var result = CustomSetService.BuildOptimalSet(users);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.DesignId == "A" && p.ColorCode == 1 && p.Quantity == 10);
        Assert.Contains(result, p => p.DesignId == "B" && p.ColorCode == 2 && p.Quantity == 5);
    }

    [Fact]
    public void BuildOptimalSet_TwoUsers_TakesMinimumQuantity()
    {
        // 2 users → threshold = ceil(2/2) = 1 → index 0 in descending sort
        // Both must own the piece; quantity = min(owned by each)
        var u1 = Build.UserWith(("A", 1, 10));
        var u2 = Build.UserWith(("A", 1, 4));

        var result = CustomSetService.BuildOptimalSet([u1, u2]);

        var piece = Assert.Single(result);
        Assert.Equal(10, piece.Quantity); // index 0 of [10, 4] descending = 10
    }

    [Fact]
    public void BuildOptimalSet_ExcludesPieceOwnedByFewerThanHalf()
    {
        // 4 users → threshold = ceil(4/2) = 2 → need at least 2 owners
        // Piece A owned by only 1 user → excluded
        var u1 = Build.UserWith(("A", 1, 5));
        var u2 = Build.UserWith();
        var u3 = Build.UserWith();
        var u4 = Build.UserWith();

        var result = CustomSetService.BuildOptimalSet([u1, u2, u3, u4]);

        Assert.Empty(result);
    }

    [Fact]
    public void BuildOptimalSet_IncludesPieceOwnedByExactlyHalf()
    {
        // 4 users → threshold index = 1 → 2nd largest count
        // Piece owned by exactly 2 users (with qty 5 each), other 2 have 0
        // sorted desc: [5, 5, 0, 0] → index 1 = 5
        var u1 = Build.UserWith(("A", 1, 5));
        var u2 = Build.UserWith(("A", 1, 5));
        var u3 = Build.UserWith();
        var u4 = Build.UserWith();

        var result = CustomSetService.BuildOptimalSet([u1, u2, u3, u4]);

        var piece = Assert.Single(result);
        Assert.Equal(5, piece.Quantity);
    }

    [Fact]
    public void BuildOptimalSet_OddUserCount_UsesCorrectThreshold()
    {
        // 3 users → threshold = ceil(3/2) = 2 → index 1 in descending sort
        // Piece owned by all 3 with counts 10, 6, 2 → index 1 = 6
        var u1 = Build.UserWith(("A", 1, 10));
        var u2 = Build.UserWith(("A", 1, 6));
        var u3 = Build.UserWith(("A", 1, 2));

        var result = CustomSetService.BuildOptimalSet([u1, u2, u3]);

        var piece = Assert.Single(result);
        Assert.Equal(6, piece.Quantity);
    }

    [Fact]
    public void BuildOptimalSet_QuantityLimitedByThresholdOwner()
    {
        // 4 users → threshold index = 1 → 2nd largest
        // Counts: [10, 3, 3, 0] → index 1 = 3
        var u1 = Build.UserWith(("A", 1, 10));
        var u2 = Build.UserWith(("A", 1, 3));
        var u3 = Build.UserWith(("A", 1, 3));
        var u4 = Build.UserWith();

        var result = CustomSetService.BuildOptimalSet([u1, u2, u3, u4]);

        var piece = Assert.Single(result);
        Assert.Equal(3, piece.Quantity);
    }

    [Fact]
    public void BuildOptimalSet_DifferentColorsTrackedSeparately()
    {
        // Same piece ID but different colors → separate entries
        var u1 = Build.UserWith(("A", 1, 5), ("A", 2, 3));
        var u2 = Build.UserWith(("A", 1, 5), ("A", 2, 3));

        var result = CustomSetService.BuildOptimalSet([u1, u2]);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.DesignId == "A" && p.ColorCode == 1 && p.Quantity == 5);
        Assert.Contains(result, p => p.DesignId == "A" && p.ColorCode == 2 && p.Quantity == 3);
    }

    // ── CanBuildSpec ──────────────────────────────────────────────────────────

    [Fact]
    public void CanBuildSpec_EmptySpec_ReturnsTrue()
    {
        var user   = Build.UserWith();
        var result = CustomSetService.CanBuildSpec([], user);
        Assert.True(result);
    }

    [Fact]
    public void CanBuildSpec_UserHasExactPieces_ReturnsTrue()
    {
        var spec = new List<CustomSetPiece> { new("A", 1, 5) };
        var user = Build.UserWith(("A", 1, 5));
        Assert.True(CustomSetService.CanBuildSpec(spec, user));
    }

    [Fact]
    public void CanBuildSpec_UserHasMoreThanRequired_ReturnsTrue()
    {
        var spec = new List<CustomSetPiece> { new("A", 1, 5) };
        var user = Build.UserWith(("A", 1, 20));
        Assert.True(CustomSetService.CanBuildSpec(spec, user));
    }

    [Fact]
    public void CanBuildSpec_UserMissingPiece_ReturnsFalse()
    {
        var spec = new List<CustomSetPiece> { new("A", 1, 5) };
        var user = Build.UserWith();
        Assert.False(CustomSetService.CanBuildSpec(spec, user));
    }

    [Fact]
    public void CanBuildSpec_UserHasWrongColor_ReturnsFalse()
    {
        var spec = new List<CustomSetPiece> { new("A", 1, 5) };
        var user = Build.UserWith(("A", 2, 10));
        Assert.False(CustomSetService.CanBuildSpec(spec, user));
    }

    [Fact]
    public void CanBuildSpec_UserShortOnQuantity_ReturnsFalse()
    {
        var spec = new List<CustomSetPiece> { new("A", 1, 10) };
        var user = Build.UserWith(("A", 1, 3));
        Assert.False(CustomSetService.CanBuildSpec(spec, user));
    }
}
