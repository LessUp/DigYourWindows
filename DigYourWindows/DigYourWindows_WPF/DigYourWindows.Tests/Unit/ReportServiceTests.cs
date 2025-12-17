using DigYourWindows.Core.Models;
using DigYourWindows.Core.Services;

namespace DigYourWindows.Tests.Unit;

public class ReportServiceTests
{
    [Fact]
    public void SerializeToJson_ThenDeserialize_ShouldPreserveSelectedFields()
    {
        var service = new ReportService();
        var collectedAt = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        var data = new DiagnosticData
        {
            Hardware = new HardwareData
            {
                ComputerName = "TEST-PC",
                OsVersion = "Windows 11",
                CpuBrand = "Test CPU",
                CpuCores = 8,
                TotalMemory = 8UL * 1024UL * 1024UL * 1024UL,
                Disks = new List<DiskInfoData>
                {
                    new DiskInfoData
                    {
                        Name = "C:",
                        FileSystem = "NTFS",
                        TotalSpace = 100UL,
                        AvailableSpace = 50UL
                    }
                },
                DiskSmart = new List<DiskSmartData>
                {
                    new DiskSmartData
                    {
                        DeviceId = "0",
                        FriendlyName = "TestDisk",
                        SerialNumber = "SN123",
                        BusType = 11,
                        MediaType = 4,
                        Size = 512UL * 1024UL * 1024UL * 1024UL,
                        HealthStatus = 1,
                        Temperature = 40,
                        Wear = 5,
                        PowerOnHours = 1234
                    }
                }
            },
            Reliability = new List<ReliabilityRecordData>
            {
                new ReliabilityRecordData
                {
                    Timestamp = collectedAt,
                    SourceName = "TestSource",
                    Message = "TestMessage",
                    EventType = "Error",
                    RecordType = 1
                }
            },
            Events = new List<LogEventData>
            {
                new LogEventData
                {
                    TimeGenerated = collectedAt,
                    LogFile = "System",
                    SourceName = "Kernel-Power",
                    EventType = "Error",
                    EventId = 41,
                    Message = "Test event message"
                }
            },
            Performance = new PerformanceAnalysisData
            {
                SystemHealthScore = 80,
                StabilityScore = 80,
                PerformanceScore = 80,
                MemoryUsageScore = 75,
                DiskHealthScore = 70,
                SystemUptimeDays = 1,
                CriticalIssuesCount = 1,
                WarningsCount = 0,
                Recommendations = new List<string> { "Rec1" },
                HealthGrade = "良好",
                HealthColor = "#17a2b8"
            },
            CollectedAt = collectedAt
        };

        var json = service.SerializeToJson(data, indented: false);
        var deserialized = service.DeserializeFromJson(json);

        Assert.NotNull(deserialized);
        Assert.Equal(data.Hardware.ComputerName, deserialized!.Hardware.ComputerName);
        Assert.Equal(data.Hardware.OsVersion, deserialized.Hardware.OsVersion);
        Assert.Equal(data.Hardware.CpuBrand, deserialized.Hardware.CpuBrand);
        Assert.Equal(data.Hardware.CpuCores, deserialized.Hardware.CpuCores);
        Assert.Equal(data.Hardware.TotalMemory, deserialized.Hardware.TotalMemory);

        Assert.Single(deserialized.Hardware.Disks);
        Assert.Equal("C:", deserialized.Hardware.Disks[0].Name);

        Assert.Single(deserialized.Hardware.DiskSmart);
        Assert.Equal("TestDisk", deserialized.Hardware.DiskSmart[0].FriendlyName);

        Assert.Single(deserialized.Events);
        Assert.Equal((uint)41, deserialized.Events[0].EventId);

        Assert.Single(deserialized.Reliability);
        Assert.Equal(1, deserialized.Reliability[0].RecordType);

        Assert.Equal(80, deserialized.Performance.SystemHealthScore);
        Assert.Equal(collectedAt, deserialized.CollectedAt);
    }

    [Fact]
    public void SerializeToJson_ShouldIncludeDiskSmartProperty()
    {
        var service = new ReportService();

        var data = new DiagnosticData
        {
            Hardware = new HardwareData
            {
                ComputerName = "TEST-PC",
                DiskSmart = new List<DiskSmartData>
                {
                    new DiskSmartData
                    {
                        DeviceId = "0",
                        FriendlyName = "TestDisk"
                    }
                }
            },
            CollectedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var json = service.SerializeToJson(data, indented: false);

        Assert.Contains("\"diskSmart\"", json);
        Assert.Contains("\"TestDisk\"", json);
    }

    [Fact]
    public void GenerateHtmlReport_ShouldContainKeySectionsAndTruncateLongEventMessage()
    {
        var service = new ReportService();
        var collectedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var truncatedSuffix = "UNIQUE_SUFFIX_SHOULD_NOT_APPEAR";
        var longMessage = new string('A', 100) + truncatedSuffix;

        var data = new DiagnosticData
        {
            Hardware = new HardwareData
            {
                ComputerName = "TEST-PC",
                OsVersion = "Windows 11",
                CpuBrand = "Test CPU",
                TotalMemory = 8UL * 1024UL * 1024UL * 1024UL,
                Gpus = new List<GpuInfoData>
                {
                    new GpuInfoData
                    {
                        Name = "GPU1",
                        Temperature = 50,
                        Load = 10,
                        MemoryUsed = 100,
                        MemoryTotal = 200,
                        CoreClock = 1000,
                        Power = 50
                    }
                }
            },
            Events = new List<LogEventData>
            {
                new LogEventData
                {
                    TimeGenerated = collectedAt,
                    LogFile = "System",
                    SourceName = "Kernel-Power",
                    EventType = "Error",
                    EventId = 41,
                    Message = longMessage
                }
            },
            Performance = new PerformanceAnalysisData
            {
                SystemHealthScore = 88,
                StabilityScore = 80,
                PerformanceScore = 80,
                MemoryUsageScore = 75,
                DiskHealthScore = 70,
                CriticalIssuesCount = 1,
                WarningsCount = 0,
                HealthGrade = "良好",
                HealthColor = "#17a2b8"
            },
            CollectedAt = collectedAt
        };

        var html = service.GenerateHtmlReport(data, daysBackForEvents: 3);

        Assert.Contains("Windows 诊断报告", html);
        Assert.Contains("系统概览", html);
        Assert.Contains(data.Hardware.ComputerName, html);
        Assert.Contains(data.Hardware.OsVersion, html);
        Assert.Contains(data.Hardware.CpuName, html);
        Assert.Contains($"{data.Hardware.TotalMemoryMB} MB", html);

        Assert.Contains("系统性能分析", html);
        Assert.Contains(data.Performance.HealthGrade, html);
        Assert.Contains(data.Performance.HealthColor, html);

        Assert.Contains("GPU 信息", html);
        Assert.Contains("GPU1", html);

        Assert.Contains("错误日志 (最近3天)", html);
        Assert.DoesNotContain(truncatedSuffix, html);
    }
}
