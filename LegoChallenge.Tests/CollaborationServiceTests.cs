using LegoChallenge.Client.Services;
using Xunit;

namespace LegoChallenge.Tests;

public class CollaborationServiceTests
{
    [Fact]
    public void Solvable_PrimaryCanBuildAlone()
    {
        var set     = Build.Set(Build.Piece("A", 1, 5));
        var primary = Build.UserWith(("A", 1, 5));

        var result = CollaborationService.FindMinimumCollaborators(set, primary, []);

        Assert.True(result.Solvable);
        Assert.Empty(result.Collaborators);
        Assert.Empty(result.StillMissing);
    }

    [Fact]
    public void Solvable_OneCollaboratorFillsGap()
    {
        var set     = Build.Set(Build.Piece("A", 1, 10));
        var primary = Build.UserWith(("A", 1, 3));
        var collab  = Build.UserWith(("A", 1, 20));

        var result = CollaborationService.FindMinimumCollaborators(
            set, primary, [(Build.SummaryOf(collab), collab)]);

        Assert.True(result.Solvable);
        Assert.Single(result.Collaborators);
        Assert.Empty(result.StillMissing);
    }

    [Fact]
    public void Solvable_TwoCollaboratorsNeeded()
    {
        var set     = Build.Set(Build.Piece("A", 1, 5), Build.Piece("B", 2, 5));
        var primary = Build.UserWith();
        var c1      = Build.UserWith(("A", 1, 5));
        var c2      = Build.UserWith(("B", 2, 5));

        var result = CollaborationService.FindMinimumCollaborators(
            set, primary, [(Build.SummaryOf(c1), c1), (Build.SummaryOf(c2), c2)]);

        Assert.True(result.Solvable);
        Assert.Equal(2, result.Collaborators.Count);
        Assert.Empty(result.StillMissing);
    }

    [Fact]
    public void NotSolvable_NobodyHasMissingPiece()
    {
        var set     = Build.Set(Build.Piece("A", 1, 5));
        var primary = Build.UserWith();
        var collab  = Build.UserWith(("B", 1, 99)); // owns B, not A

        var result = CollaborationService.FindMinimumCollaborators(
            set, primary, [(Build.SummaryOf(collab), collab)]);

        Assert.False(result.Solvable);
        Assert.Single(result.StillMissing);
        Assert.Equal("A", result.StillMissing[0].DesignId);
    }

    [Fact]
    public void Greedy_PicksCollaboratorWithBestCoverage()
    {
        // Primary missing A and B. c1 covers both; c2 covers only A.
        // Greedy should pick c1 first and solve with just 1 collaborator.
        var set     = Build.Set(Build.Piece("A", 1, 5), Build.Piece("B", 2, 5));
        var primary = Build.UserWith();
        var c1      = Build.UserWith(("A", 1, 5), ("B", 2, 5));
        var c2      = Build.UserWith(("A", 1, 5));

        var result = CollaborationService.FindMinimumCollaborators(
            set, primary, [(Build.SummaryOf(c1), c1), (Build.SummaryOf(c2), c2)]);

        Assert.True(result.Solvable);
        Assert.Single(result.Collaborators);
        Assert.Equal(c1.Id, result.Collaborators[0].User.Id);
    }

    [Fact]
    public void Contribution_CapedAtDeficit()
    {
        // Primary short by 7. Collaborator has 20. Contribution should be 7, not 20.
        var set     = Build.Set(Build.Piece("A", 1, 10));
        var primary = Build.UserWith(("A", 1, 3));
        var collab  = Build.UserWith(("A", 1, 20));

        var result = CollaborationService.FindMinimumCollaborators(
            set, primary, [(Build.SummaryOf(collab), collab)]);

        Assert.True(result.Solvable);
        Assert.Equal(7, result.Collaborators[0].Pieces[0].Quantity);
    }

    [Fact]
    public void Solvable_WhenMultipleCollaboratorsCoverPartialDeficit()
    {
        // Primary missing 10 of A. c1 has 6, c2 has 6. Together they cover the gap.
        var set     = Build.Set(Build.Piece("A", 1, 10));
        var primary = Build.UserWith();
        var c1      = Build.UserWith(("A", 1, 6));
        var c2      = Build.UserWith(("A", 1, 6));

        var result = CollaborationService.FindMinimumCollaborators(
            set, primary, [(Build.SummaryOf(c1), c1), (Build.SummaryOf(c2), c2)]);

        Assert.True(result.Solvable);
        Assert.Equal(2, result.Collaborators.Count);
    }
}
