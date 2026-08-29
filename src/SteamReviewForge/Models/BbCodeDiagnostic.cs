namespace SteamReviewForge.Models;

public enum BbCodeDiagnosticSeverity
{
    Warning
}

public sealed record BbCodeDiagnostic(
    int Line,
    int Column,
    string Message,
    BbCodeDiagnosticSeverity Severity = BbCodeDiagnosticSeverity.Warning);

public sealed class BbCodeAnalysisResult
{
    public List<BbCodeDiagnostic> Diagnostics { get; } = [];

    public bool HasWarnings => Diagnostics.Count > 0;
}
