using System.Text.Json.Serialization;

namespace LegoChallenge.Client.Models;

// Response wrappers are private to LegoApiService — not public domain types.

// ── User ───────────────────────────────────────────────────────────────────

public record UserSummary(string Id, string Username, string Location, int BrickCount);

public record User(
    string Id,
    string Username,
    string Location,
    int BrickCount,
    List<PieceStock> Collection);

public record PieceStock(string PieceId, List<PieceVariant> Variants);

public record PieceVariant([property: JsonPropertyName("color")] int ColorCode, int Count);

// ── Sets ───────────────────────────────────────────────────────────────────

public record SetSummary(string Id, string Name, string SetNumber, int TotalPieces);

public record LegoSet(
    string Id,
    string Name,
    string SetNumber,
    int TotalPieces,
    List<SetPiece> Pieces);

public record SetPiece(PartInfo Part, int Quantity);

public record PartInfo(
    [property: JsonPropertyName("designID")] string DesignId,
    [property: JsonPropertyName("material")] int ColorCode,
    string PartType);

// ── Colours ────────────────────────────────────────────────────────────────

public record Colour(string Name, int Code);

// ── Analysis results ───────────────────────────────────────────────────────

public record BuildResult(SetSummary Set, bool CanBuild, List<MissingPiece> Missing);

public record MissingPiece(string DesignId, int ColorCode, int Required, int Have)
{
    public int ShortBy => Required - Have;
}

// ── Collaboration ──────────────────────────────────────────────────────────

public record ContributedPiece(string DesignId, int ColorCode, int Quantity);
public record CollaboratorContribution(UserSummary User, List<ContributedPiece> Pieces);
public record CollaborationResult(bool Solvable, List<CollaboratorContribution> Collaborators, List<MissingPiece> StillMissing);
public record CollaborationCombination(List<CollaboratorContribution> Contributors);

// ── Colour substitution ────────────────────────────────────────────────────

public record SubstitutionInfo(int OriginalColor, int SubstituteColor);
public record SubstitutedBuildResult(bool CanBuild, List<MissingPiece> Missing, List<SubstitutionInfo> Substitutions);

// ── Custom set ─────────────────────────────────────────────────────────────

public record CustomSetPiece(string DesignId, int ColorCode, int Quantity);
