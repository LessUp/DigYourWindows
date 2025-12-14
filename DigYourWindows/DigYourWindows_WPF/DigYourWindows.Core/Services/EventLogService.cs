using System.Diagnostics;
using DigYourWindows.Core.Models;
using SysEventLogEntry = System.Diagnostics.EventLogEntry;

namespace DigYourWindows.Core.Services;

public class EventLogService
{
    public List<LogEventData> GetErrorEvents(int daysBack = 3)
    {
        var events = new List<LogEventData>();
        var cutoffDate = DateTime.Now.AddDays(-daysBack);

        try
        {
            // System Log
            events.AddRange(ReadEventLog("System", cutoffDate));
            
            // Application Log
            events.AddRange(ReadEventLog("Application", cutoffDate));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"读取事件日志失败: {ex.Message}");
        }

        return events.OrderByDescending(e => e.TimeGenerated).ToList();
    }

    private List<LogEventData> ReadEventLog(string logName, DateTime cutoffDate)
    {
        var entries = new List<LogEventData>();
        
        try
        {
            using var eventLog = new EventLog(logName);
            
            foreach (SysEventLogEntry entry in eventLog.Entries)
            {
                if (entry.TimeGenerated < cutoffDate)
                    continue;

                // Only Error and Warning
                if (entry.EntryType != EventLogEntryType.Error && 
                    entry.EntryType != EventLogEntryType.Warning)
                    continue;

                var instanceId = entry.InstanceId;
                entries.Add(new LogEventData
                {
                    TimeGenerated = entry.TimeGenerated,
                    SourceName = entry.Source,
                    Message = entry.Message,
                    EventType = entry.EntryType.ToString(),
                    LogFile = logName,
                    EventId = instanceId < 0 ? 0u : (uint)Math.Min((ulong)instanceId, uint.MaxValue)
                });
            }
        }
        catch { }

        return entries;
    }
}
