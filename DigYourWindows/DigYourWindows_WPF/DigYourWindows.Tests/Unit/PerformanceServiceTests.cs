using DigYourWindows.Core.Models;
using DigYourWindows.Core.Services;

namespace DigYourWindows.Tests.Unit;

public class PerformanceServiceTests
{
    [Fact]
    public void AnalyzeSystemPerformance_PoorSystem_ShouldReturnLowGradeAndRecommendations()
    {
        var hardware = new HardwareData
        {
            CpuBrand = "Unknown",
            CpuCores = 1,
            TotalMemory = 2UL * 1024UL * 1024UL * 1024UL,
            Disks = new List<DiskInfoData>
            {
                new DiskInfoData
                {
                    Name = "C:",
                    FileSystem = "NTFS",
                    TotalSpace = 100UL,
                    AvailableSpace = 5UL
                }
            }
        };

        var events = new List<LogEventData>
        {
            new LogEventData
            {
                TimeGenerated = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                LogFile = "System",
                SourceName = "Kernel-Power",
                EventType = "Error",
                EventId = 41,
                Message = "Test critical error"
            }
        };

        var reliability = Enumerable
            .Range(0, 51)
            .Select(i => new ReliabilityRecordData
            {
                Timestamp = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(i),
                SourceName = "TestSource",
                Message = "TestMessage",
                EventType = "Error",
                RecordType = 1
            })
            .ToList();

        var service = new PerformanceService();
        var analysis = service.AnalyzeSystemPerformance(hardware, events, reliability);

        Assert.InRange(analysis.SystemHealthScore, 0d, 100d);
        Assert.True(analysis.SystemHealthScore < 60d);
        Assert.Equal("较差", analysis.HealthGrade);
        Assert.Equal("#fd7e14", analysis.HealthColor);

        Assert.Contains("内存容量严重不足，强烈建议升级到8GB或更多", analysis.Recommendations);
        Assert.Contains("磁盘 C: 剩余空间严重不足 (5%)，请立即清理空间", analysis.Recommendations);
        Assert.Contains("系统可靠性记录较多，建议检查系统稳定性", analysis.Recommendations);
        Assert.Contains("CPU核心数较少，可能会影响多任务处理性能", analysis.Recommendations);
        Assert.Contains("发现 1 个严重系统错误，建议立即检查系统日志", analysis.Recommendations);
        Assert.Contains("系统健康评分较低，建议进行全面系统维护", analysis.Recommendations);

        Assert.Equal((uint)1, analysis.CriticalIssuesCount);
        Assert.Equal((uint)0, analysis.WarningsCount);
    }

    [Fact]
    public void AnalyzeSystemPerformance_HighEndSystem_ShouldReturnExcellentGradeAndNoRecommendations()
    {
        var hardware = new HardwareData
        {
            CpuBrand = "Intel(R) Core(TM) i9-12900K",
            CpuCores = 8,
            TotalMemory = 16UL * 1024UL * 1024UL * 1024UL,
            Disks = new List<DiskInfoData>
            {
                new DiskInfoData
                {
                    Name = "C:",
                    FileSystem = "NTFS",
                    TotalSpace = 100UL,
                    AvailableSpace = 60UL
                }
            }
        };

        var service = new PerformanceService();
        var analysis = service.AnalyzeSystemPerformance(hardware, new List<LogEventData>(), new List<ReliabilityRecordData>());

        Assert.True(analysis.SystemHealthScore >= 90d);
        Assert.Equal("优秀", analysis.HealthGrade);
        Assert.Equal("#28a745", analysis.HealthColor);
        Assert.Empty(analysis.Recommendations);

        Assert.Equal((uint)0, analysis.CriticalIssuesCount);
        Assert.Equal((uint)0, analysis.WarningsCount);
    }
}
