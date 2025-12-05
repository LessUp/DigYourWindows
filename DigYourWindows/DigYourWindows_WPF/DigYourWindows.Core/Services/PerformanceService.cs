using System.Diagnostics;
using System.Management;
using DigYourWindows.Core.Models;

namespace DigYourWindows.Core.Services;

public class PerformanceService
{
    /// <summary>
    /// 分析系统性能和健康状况
    /// </summary>
    /// <param name="hardware">硬件信息</param>
    /// <param name="events">事件日志</param>
    /// <param name="reliability">可靠性记录</param>
    /// <returns>性能分析结果</returns>
    public PerformanceAnalysis AnalyzeSystemPerformance(
        HardwareInfo hardware,
        List<Models.EventLogEntry> events,
        List<ReliabilityRecord> reliability)
    {
        var performanceMetrics = new Dictionary<string, float>();
        var recommendations = new List<string>();

        // 分析事件日志以获取详细统计信息
        var eventAnalysis = AnalyzeEvents(events);
        
        // 计算内存使用评分
        var totalMemoryGB = hardware.TotalMemoryMB / 1024f;
        var memoryUsageScore = CalculateMemoryScore(totalMemoryGB, recommendations);
        performanceMetrics.Add("memory_score", memoryUsageScore);

        // 计算磁盘健康评分
        var diskHealthScore = CalculateDiskScore(hardware.Disks, recommendations);
        performanceMetrics.Add("disk_health", diskHealthScore);

        // 获取系统运行时间（天数）
        var systemUptimeDays = GetSystemUptimeDays();
        
        // 计算系统稳定性评分
        var stabilityScore = CalculateStabilityScore(
            eventAnalysis.ErrorCount,
            eventAnalysis.WarningCount,
            eventAnalysis.CriticalEvents.Count,
            reliability.Count,
            recommendations);
        performanceMetrics.Add("stability", stabilityScore);

        // 计算性能评分
        var performanceScore = CalculatePerformanceScore(
            hardware.CpuCores,
            hardware.CpuName,
            totalMemoryGB,
            recommendations);
        performanceMetrics.Add("performance", performanceScore);

        // 计算整体系统健康评分
        var systemHealthScore = (stabilityScore * 0.4f + performanceScore * 0.3f +
                                  memoryUsageScore * 0.15f + diskHealthScore * 0.15f);
        systemHealthScore = Math.Max(0f, Math.Min(100f, systemHealthScore));
        
        // 添加摘要指标
        performanceMetrics.Add("overall_health", systemHealthScore);

        // 生成额外的建议
        if (eventAnalysis.CriticalEvents.Count > 0)
        {
            recommendations.Add(
                $"发现 {eventAnalysis.CriticalEvents.Count} 个严重系统错误，建议立即检查系统日志");
        }

        if (systemHealthScore < 60f)
        {
            recommendations.Add("系统健康评分较低，建议进行全面系统维护");
        }

        return new PerformanceAnalysis
        {
            SystemHealthScore = systemHealthScore,
            StabilityScore = stabilityScore,
            PerformanceScore = performanceScore,
            MemoryUsageScore = memoryUsageScore,
            DiskHealthScore = diskHealthScore,
            SystemUptimeDays = systemUptimeDays,
            CriticalIssuesCount = eventAnalysis.CriticalEvents.Count,
            WarningsCount = eventAnalysis.WarningCount,
            PerformanceMetrics = performanceMetrics,
            Recommendations = recommendations
        };
    }

    /// <summary>
    /// 获取系统运行时间（天数）
    /// </summary>
    private float? GetSystemUptimeDays()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT LastBootUpTime FROM Win32_OperatingSystem");
            foreach (var obj in searcher.Get())
            {
                var lastBoot = ManagementDateTimeConverter.ToDateTime(obj["LastBootUpTime"]?.ToString() ?? string.Empty);
                var uptime = DateTime.Now - lastBoot;
                return (float)uptime.TotalDays;
            }
        }
        catch
        {
            // 如果无法获取，返回 null
        }
        return null;
    }

    /// <summary>
    /// 计算内存评分
    /// </summary>
    private float CalculateMemoryScore(float totalMemoryGB, List<string> recommendations)
    {
        var score = 50f; // 基础分数

        if (totalMemoryGB >= 16f)
        {
            score = 90f; // 优秀
        }
        else if (totalMemoryGB >= 8f)
        {
            score = 75f; // 良好
        }
        else if (totalMemoryGB >= 4f)
        {
            score = 60f; // 可接受
            recommendations.Add("内存容量较小，建议考虑升级到8GB或更多以提升性能");
        }
        else
        {
            score = 40f; // 较差
            recommendations.Add("内存容量严重不足，强烈建议升级到8GB或更多");
        }

        return score;
    }

    /// <summary>
    /// 计算磁盘健康评分
    /// </summary>
    private float CalculateDiskScore(List<DiskInfo> disks, List<string> recommendations)
    {
        if (disks.Count == 0)
        {
            recommendations.Add("未检测到磁盘信息，请检查磁盘连接");
            return 50f;
        }

        var totalScore = 0f;

        foreach (var disk in disks)
        {
            // 计算可用空间百分比
            var freePercentage = disk.TotalSizeGB > 0 ? (disk.FreeSpaceGB / (float)disk.TotalSizeGB) * 100f : 0f;
            var diskScore = 50f; // 基础分数

            if (freePercentage > 50f)
            {
                diskScore = 90f; // 优秀
            }
            else if (freePercentage > 25f)
            {
                diskScore = 75f; // 良好
            }
            else if (freePercentage > 10f)
            {
                diskScore = 60f; // 可接受
                recommendations.Add($"磁盘 {disk.Name} 剩余空间不足 ({freePercentage:F0}%)，建议清理空间");
            }
            else
            {
                diskScore = 30f; // 较差
                recommendations.Add($"磁盘 {disk.Name} 剩余空间严重不足 ({freePercentage:F0}%)，请立即清理空间");
            }

            totalScore += diskScore;
        }

        return totalScore / disks.Count;
    }

    /// <summary>
    /// 计算稳定性评分
    /// </summary>
    private float CalculateStabilityScore(
        int errorCount,
        int warningCount,
        int criticalEventsCount,
        int reliabilityRecordsCount,
        List<string> recommendations)
    {
        var score = 100f; // 从完美分数开始

        // 根据错误数量扣分
        score -= Math.Min(40f, errorCount * 2f);

        // 根据警告数量扣分
        score -= Math.Min(20f, warningCount * 0.5f);

        // 根据严重事件扣分
        score -= Math.Min(30f, criticalEventsCount * 10f);

        // 检查可靠性问题
        if (reliabilityRecordsCount > 50)
        {
            score -= 10f;
            recommendations.Add("系统可靠性记录较多，建议检查系统稳定性");
        }

        return Math.Max(0f, score);
    }

    /// <summary>
    /// 计算性能评分
    /// </summary>
    private float CalculatePerformanceScore(
        int cpuCount,
        string cpuBrand,
        float totalMemoryGB,
        List<string> recommendations)
    {
        var score = 50f; // 基础分数
        var brand = cpuBrand.ToLowerInvariant();

        // CPU 性能评估
        if (cpuCount >= 8)
        {
            score += 20f; // 优秀
        }
        else if (cpuCount >= 4)
        {
            score += 15f; // 良好
        }
        else if (cpuCount >= 2)
        {
            score += 5f; // 可接受
        }
        else
        {
            score -= 10f; // 较差
            recommendations.Add("CPU核心数较少，可能会影响多任务处理性能");
        }

        // CPU 品牌评估
        if (brand.Contains("intel"))
        {
            if (brand.Contains("i9") || brand.Contains("xeon"))
            {
                score += 15f; // 高端
            }
            else if (brand.Contains("i7"))
            {
                score += 10f; // 中高端
            }
            else if (brand.Contains("i5"))
            {
                score += 5f; // 中端
            }
        }
        else if (brand.Contains("amd"))
        {
            if (brand.Contains("ryzen 9") || brand.Contains("threadripper"))
            {
                score += 15f; // 高端
            }
            else if (brand.Contains("ryzen 7"))
            {
                score += 10f; // 中高端
            }
            else if (brand.Contains("ryzen 5"))
            {
                score += 5f; // 中端
            }
        }

        // 内存评估
        if (totalMemoryGB >= 16f)
        {
            score += 15f; // 优秀
        }
        else if (totalMemoryGB >= 8f)
        {
            score += 10f; // 良好
        }
        else if (totalMemoryGB >= 4f)
        {
            score += 5f; // 可接受
        }
        else
        {
            score -= 5f; // 较差
        }

        return Math.Max(0f, Math.Min(100f, score));
    }

    /// <summary>
    /// 分析事件日志
    /// </summary>
    private EventAnalysisResult AnalyzeEvents(List<Models.EventLogEntry> events)
    {
        var errorCount = 0;
        var warningCount = 0;
        var criticalEvents = new List<Models.EventLogEntry>();

        foreach (var evt in events)
        {
            switch (evt.EventType.ToLowerInvariant())
            {
                case "error":
                    errorCount++;
                    // 判断是否为关键错误
                    if (IsCriticalError(evt))
                    {
                        criticalEvents.Add(evt);
                    }
                    break;
                case "warning":
                    warningCount++;
                    break;
            }
        }

        return new EventAnalysisResult
        {
            ErrorCount = errorCount,
            WarningCount = warningCount,
            CriticalEvents = criticalEvents
        };
    }

    /// <summary>
    /// 判断是否为关键错误
    /// </summary>
    private bool IsCriticalError(Models.EventLogEntry evt)
    {
        // 定义一些关键错误的事件ID
        var criticalEventIds = new[] { 41, 55, 57, 1003, 1073, 6008, 7034, 7036 };
        
        return criticalEventIds.Contains(evt.EventId) ||
               evt.Source.ToLowerInvariant().Contains("bugcheck") ||
               evt.Message.ToLowerInvariant().Contains("critical") ||
               evt.Message.ToLowerInvariant().Contains("fatal");
    }

    /// <summary>
    /// 事件分析结果
    /// </summary>
    private record EventAnalysisResult
    {
        public int ErrorCount { get; init; }
        public int WarningCount { get; init; }
        public List<Models.EventLogEntry> CriticalEvents { get; init; } = new();
    }
}