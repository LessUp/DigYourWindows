using System.Diagnostics;
using DigYourWindows.Core.Models;
using SysEventLogEntry = System.Diagnostics.EventLogEntry;

namespace DigYourWindows.Core.Services;

public class EventLogService
{
    public List<Models.EventLogEntry> GetErrorEvents(int daysBack = 3)
    {
        var events = new List<Models.EventLogEntry>();
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

    private List<Models.EventLogEntry> ReadEventLog(string logName, DateTime cutoffDate)
    {
        var entries = new List<Models.EventLogEntry>();
        
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

                entries.Add(new Models.EventLogEntry
                {
                    TimeGenerated = entry.TimeGenerated,
                    Source = entry.Source,
                    Message = entry.Message,
                    EventType = entry.EntryType.ToString(),
                    LogName = logName,
                    EventId = (int)entry.InstanceId
                });
            }
        }
        catch { }

        return entries;
    }
}
