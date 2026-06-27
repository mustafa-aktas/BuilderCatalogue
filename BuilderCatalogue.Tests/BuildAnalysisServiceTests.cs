using BuilderCatalogue.Client.Services;
using Xunit;

namespace BuilderCatalogue.Tests;

public class BuildAnalysisServiceTests
{
    [Fact]
    public void CanBuild_WhenUserHasExactQuantities()
    {
        var set  = Build.Set(Build.Piece("A", 1, 10), Build.Piece("B", 1, 5));
        var user = Build.UserWith(("A", 1, 10), ("B", 1, 5));

        var result = BuildAnalysisService.Analyze(Build.Summary(set), set, user);

        Assert.True(result.CanBuild);
        Assert.Empty(result.Missing);
    }

    [Fact]
    public void CanBuild_WhenUserHasMoreThanRequired()
    {
        var set  = Build.Set(Build.Piece("A", 1, 5));
        var user = Build.UserWith(("A", 1, 20));

        var result = BuildAnalysisService.Analyze(Build.Summary(set), set, user);

        Assert.True(result.CanBuild);
    }

    [Fact]
    public void CanBuild_EmptySet()
    {
        var set  = Build.Set();
        var user = Build.UserWith();

        var result = BuildAnalysisService.Analyze(Build.Summary(set), set, user);

        Assert.True(result.CanBuild);
        Assert.Empty(result.Missing);
    }

    [Fact]
    public void CannotBuild_WhenPieceCompletelyAbsent()
    {
        var set  = Build.Set(Build.Piece("A", 1, 5));
        var user = Build.UserWith();

        var result = BuildAnalysisService.Analyze(Build.Summary(set), set, user);

        Assert.False(result.CanBuild);
        Assert.Single(result.Missing);
        Assert.Equal("A",  result.Missing[0].DesignId);
        Assert.Equal(5,    result.Missing[0].Required);
        Assert.Equal(0,    result.Missing[0].Have);
        Assert.Equal(5,    result.Missing[0].ShortBy);
    }

    [Fact]
    public void CannotBuild_WhenInsufficientQuantity()
    {
        var set  = Build.Set(Build.Piece("A", 1, 10));
        var user = Build.UserWith(("A", 1, 3));

        var result = BuildAnalysisService.Analyze(Build.Summary(set), set, user);

        Assert.False(result.CanBuild);
        Assert.Equal(10, result.Missing[0].Required);
        Assert.Equal(3,  result.Missing[0].Have);
        Assert.Equal(7,  result.Missing[0].ShortBy);
    }

    [Fact]
    public void CannotBuild_WhenWrongColorOwned()
    {
        var set  = Build.Set(Build.Piece("A", 1, 5));
        var user = Build.UserWith(("A", 2, 10)); // owns color 2, needs color 1

        var result = BuildAnalysisService.Analyze(Build.Summary(set), set, user);

        Assert.False(result.CanBuild);
        Assert.Equal(1, result.Missing[0].ColorCode);
    }

    [Fact]
    public void Missing_ContainsAllShortPieces()
    {
        var set  = Build.Set(Build.Piece("A", 1, 5), Build.Piece("B", 2, 3));
        var user = Build.UserWith();

        var result = BuildAnalysisService.Analyze(Build.Summary(set), set, user);

        Assert.False(result.CanBuild);
        Assert.Equal(2, result.Missing.Count);
    }
}
