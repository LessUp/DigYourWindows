namespace DigYourWindows.Core.Models;

public record ReliabilityRecord
{
    public DateTime TimeGenerated { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public int RecordType { get; init; } // 1=App failure, 2=Windows failure, etc.
    public string RecordTypeDescription => RecordType switch
    {
        1 => "应用程序故障",
        2 => "Windows 故障",
        3 => "其他故障",
        _ => "未知"
    };
}
