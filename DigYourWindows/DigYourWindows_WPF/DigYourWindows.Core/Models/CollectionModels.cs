namespace DigYourWindows.Core.Models;

public readonly record struct DiagnosticCollectionProgress(int StepIndex, int StepCount, string Message);

public sealed record DiagnosticCollectionResult(DiagnosticData Data, IReadOnlyList<string> Warnings)
{
    public bool HasWarnings => Warnings.Count > 0;
}
