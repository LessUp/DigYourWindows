using System.Management;
using DigYourWindows.Core.Models;

namespace DigYourWindows.Core.Services;

public class ReliabilityService
{
    public List<ReliabilityRecord> GetReliabilityRecords(int daysBack = 7)
    {
        var records = new List<ReliabilityRecord>();
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT TimeGenerated, ProductName, Message, RecordType FROM Win32_ReliabilityRecords");
            
            var cutoffDate = DateTime.Now.AddDays(-daysBack);
            
            foreach (ManagementObject obj in searcher.Get())
            {
                var timeStr = obj["TimeGenerated"]?.ToString();
                if (string.IsNullOrEmpty(timeStr) || timeStr.Length < 14)
                    continue;

                // WMI datetime format: yyyyMMddHHmmss.ffffff+UUU
                var timeGenerated = ParseWmiDateTime(timeStr);
                
                if (timeGenerated < cutoffDate)
                    continue;

                records.Add(new ReliabilityRecord
                {
                    TimeGenerated = timeGenerated,
                    ProductName = obj["ProductName"]?.ToString() ?? "",
                    Message = obj["Message"]?.ToString() ?? "",
                    RecordType = Convert.ToInt32(obj["RecordType"] ?? 0)
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取可靠性记录失败: {ex.Message}");
        }
        return records.OrderByDescending(r => r.TimeGenerated).ToList();
    }

    private DateTime ParseWmiDateTime(string wmiDateTime)
    {
        try
        {
            // Format: yyyyMMddHHmmss.ffffff+UUU
            var year = int.Parse(wmiDateTime.Substring(0, 4));
            var month = int.Parse(wmiDateTime.Substring(4, 2));
            var day = int.Parse(wmiDateTime.Substring(6, 2));
            var hour = int.Parse(wmiDateTime.Substring(8, 2));
            var minute = int.Parse(wmiDateTime.Substring(10, 2));
            var second = int.Parse(wmiDateTime.Substring(12, 2));
            
            return new DateTime(year, month, day, hour, minute, second);
        }
        catch
        {
            return DateTime.MinValue;
        }
    }
}
