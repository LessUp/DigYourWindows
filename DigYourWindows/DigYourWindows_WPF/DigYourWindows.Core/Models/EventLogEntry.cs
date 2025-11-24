namespace DigYourWindows.Core.Models;

public record EventLogEntry
{
    public DateTime TimeGenerated { get; init; }
    public string Source { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public string LogName { get; init; } = string.Empty;
    public int EventId { get; init; }
}
