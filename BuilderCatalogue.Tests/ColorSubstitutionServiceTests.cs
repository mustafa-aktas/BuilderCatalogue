using BuilderCatalogue.Client.Services;
using Xunit;

namespace BuilderCatalogue.Tests;

public class ColorSubstitutionServiceTests
{
    [Fact]
    public void CanBuild_NoSubstitutionNeeded()
    {
        var set  = Build.Set(Build.Piece("A", 1, 10), Build.Piece("B", 1, 5));
        var user = Build.UserWith(("A", 1, 10), ("B", 1, 5));

        var result = ColorSubstitutionService.Analyze(set, user);

        Assert.True(result.CanBuild);
        Assert.Empty(result.Substitutions);
        Assert.Empty(result.Missing);
    }

    [Fact]
    public void CanBuild_SingleColorGroupSubstituted()
    {
        // Set uses color 1. User missing A/color1 but has A/color8 and B/color8.
        var set  = Build.Set(Build.Piece("A", 1, 10), Build.Piece("B", 1, 5));
        var user = Build.UserWith(("A", 8, 10), ("B", 8, 5));

        var result = ColorSubstitutionService.Analyze(set, user);

        Assert.True(result.CanBuild);
        Assert.Single(result.Substitutions);
        Assert.Equal(1, result.Substitutions[0].OriginalColor);
        Assert.Equal(8, result.Substitutions[0].SubstituteColor);
    }

    [Fact]
    public void CanBuild_TwoIndependentColorGroupsSubstituted()
    {
        // Set has color1 group and color5 group; user owns both in color8 and color9.
        var set  = Build.Set(Build.Piece("A", 1, 5), Build.Piece("B", 5, 5));
        var user = Build.UserWith(("A", 8, 5), ("B", 9, 5));

        var result = ColorSubstitutionService.Analyze(set, user);

        Assert.True(result.CanBuild);
        Assert.Equal(2, result.Substitutions.Count);
    }

    [Fact]
    public void CannotBuild_SubstituteColorAlreadyInSet()
    {
        // Set has color1 and color8. User missing A/color1 but only has A/color8.
        // color8 is already in the set → cannot substitute.
        var set  = Build.Set(Build.Piece("A", 1, 5), Build.Piece("C", 8, 3));
        var user = Build.UserWith(("A", 8, 5), ("C", 8, 3));

        var result = ColorSubstitutionService.Analyze(set, user);

        Assert.False(result.CanBuild);
        Assert.Empty(result.Substitutions);
        Assert.NotEmpty(result.Missing);
    }

    [Fact]
    public void CannotBuild_SubstituteDoesNotCoverAllPiecesInGroup()
    {
        // color1 group has pieces A and B. color8 covers A but not B.
        var set  = Build.Set(Build.Piece("A", 1, 5), Build.Piece("B", 1, 5));
        var user = Build.UserWith(("A", 8, 5)); // B/color8 missing

        var result = ColorSubstitutionService.Analyze(set, user);

        Assert.False(result.CanBuild);
        Assert.Empty(result.Substitutions);
    }

    [Fact]
    public void CanBuild_BipartiteMatchingRequiredWhenGreedyWouldFail()
    {
        // color1 can sub with color8 OR color9.
        // color5 can ONLY sub with color8.
        // Greedy (first found) would assign color8→color1, leaving color5 unmatched.
        // Matching must backtrack: assign color9→color1, color8→color5.
        var set = Build.Set(
            Build.Piece("A", 1, 5),
            Build.Piece("B", 5, 5));

        var user = Build.UserWith(
            ("A", 8, 5),  // color1 can use color8
            ("A", 9, 5),  // color1 can also use color9
            ("B", 8, 5)); // color5 can ONLY use color8

        var result = ColorSubstitutionService.Analyze(set, user);

        Assert.True(result.CanBuild);
        Assert.Equal(2, result.Substitutions.Count);

        var sub1 = result.Substitutions.Single(s => s.OriginalColor == 1);
        var sub5 = result.Substitutions.Single(s => s.OriginalColor == 5);
        Assert.Equal(9, sub1.SubstituteColor); // color9 freed up color8 for color5
        Assert.Equal(8, sub5.SubstituteColor);
    }

    [Fact]
    public void CannotBuild_IncompleteMatch_NoPartialSubstitutionsReturned()
    {
        // color1 and color5 both need substitution; only color8 available.
        // Only one can be matched → incomplete → CanBuild=false, Substitutions=[].
        var set = Build.Set(
            Build.Piece("A", 1, 5),
            Build.Piece("B", 5, 5));

        var user = Build.UserWith(
            ("A", 8, 5),  // color1 can use color8
            ("B", 8, 5)); // color5 can also use color8 — but same substitute

        var result = ColorSubstitutionService.Analyze(set, user);

        Assert.False(result.CanBuild);
        Assert.Empty(result.Substitutions);
        Assert.NotEmpty(result.Missing);
    }

    [Fact]
    public void CanBuild_ByReusingExistingColorsBySubstitutingThem()
    {
        var set = Build.Set(
            Build.Piece("A", 1, 5),
            Build.Piece("B", 5, 5));

        var user = Build.UserWith(
            ("A", 8, 5),
            ("A", 1, 5),
            ("B", 1, 5));

        //Substitution should free up color 1 by using color 8 for A, allowing B to use color 1.
        var result = ColorSubstitutionService.Analyze(set, user);
        Assert.True(result.CanBuild);
        Assert.Equal(2, result.Substitutions.Count);
        var sub1 = result.Substitutions.Single(s => s.OriginalColor == 1);
        var sub5 = result.Substitutions.Single(s => s.OriginalColor == 5);
    }

    [Fact]
    public void CanBuild_MutualSwap_BothShort_NoNonSetExit()
    {
        // color1 and color5 both short; each only has the other's colour as substitute.
        // No non-set-colour exit exists — must resolve via mutual swap.
        var set  = Build.Set(Build.Piece("A", 1, 5), Build.Piece("B", 5, 5));
        var user = Build.UserWith(("A", 5, 5), ("B", 1, 5));

        var result = ColorSubstitutionService.Analyze(set, user);

        Assert.True(result.CanBuild);
        Assert.Equal(2, result.Substitutions.Count);
        Assert.Contains(result.Substitutions, s => s.OriginalColor == 1 && s.SubstituteColor == 5);
        Assert.Contains(result.Substitutions, s => s.OriginalColor == 5 && s.SubstituteColor == 1);
    }

    [Fact]
    public void CanBuild_FulfilledGroupMustSwapToFreeSetColor_NoNonSetExit()
    {
        // color1 is fulfilled, color5 is short.
        // User has NO non-set-colour alternatives — only a mutual set-colour swap works.
        var set  = Build.Set(Build.Piece("A", 1, 5), Build.Piece("B", 5, 5));
        var user = Build.UserWith(("A", 1, 5), ("A", 5, 5), ("B", 1, 5));

        var result = ColorSubstitutionService.Analyze(set, user);

        Assert.True(result.CanBuild);
        Assert.Equal(2, result.Substitutions.Count);
        Assert.Contains(result.Substitutions, s => s.OriginalColor == 1);
        Assert.Contains(result.Substitutions, s => s.OriginalColor == 5);
    }

    [Fact]
    public void CannotBuild_SetColorChain_CoRequirementCannotBeSatisfied()
    {
        // color1 short, color5 fulfilled. color1 can use color5 as substitute.
        // But color5's group has no substitute of its own → co-requirement fails.
        var set  = Build.Set(Build.Piece("A", 1, 5), Build.Piece("B", 5, 5));
        var user = Build.UserWith(("A", 5, 5), ("B", 5, 5)); // color5 fulfilled, no sub for it

        var result = ColorSubstitutionService.Analyze(set, user);

        Assert.False(result.CanBuild);
        Assert.Empty(result.Substitutions);
    }

    [Fact]
    public void CannotBuild_InsufficientSubstituteQuantity()
    {
        // color8 exists but not enough pieces to cover the group.
        var set  = Build.Set(Build.Piece("A", 1, 10));
        var user = Build.UserWith(("A", 8, 3)); // needs 10, only 3 in sub color

        var result = ColorSubstitutionService.Analyze(set, user);

        Assert.False(result.CanBuild);
        Assert.Empty(result.Substitutions);
    }

    [Fact]
    public void Missing_ReportsAllShortPiecesWhenNoCompleteMatch()
    {
        var set  = Build.Set(Build.Piece("A", 1, 10), Build.Piece("B", 1, 5));
        var user = Build.UserWith(); // nothing owned, no substitute available

        var result = ColorSubstitutionService.Analyze(set, user);

        Assert.False(result.CanBuild);
        Assert.Equal(2, result.Missing.Count);
    }
}
