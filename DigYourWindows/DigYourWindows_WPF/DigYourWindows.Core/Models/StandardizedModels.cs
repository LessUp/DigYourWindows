using System.Text.Json.Serialization;

namespace DigYourWindows.Core.Models;

/// <summary>
/// Complete diagnostic data collected from the system
/// Matches the Rust DiagnosticData structure for cross-version compatibility
/// </summary>
public record DiagnosticData
{
    [JsonPropertyName("hardware")]
    public HardwareData Hardware { get; init; } = new();

    [JsonPropertyName("reliability")]
    public List<ReliabilityRecordData> Reliability { get; init; } = new();

    [JsonPropertyName("events")]
    public List<LogEventData> Events { get; init; } = new();

    [JsonPropertyName("performance")]
    public PerformanceAnalysisData Performance { get; init; } = new();

    [JsonPropertyName("collectedAt")]
    public DateTime CollectedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Hardware information collected from the system
/// </summary>
public record HardwareData
{
    [JsonPropertyName("computerName")]
    public string ComputerName { get; init; } = string.Empty;

    [JsonPropertyName("osVersion")]
    public string OsVersion { get; init; } = string.Empty;

    [JsonPropertyName("cpuBrand")]
    public string CpuBrand { get; init; } = string.Empty;

    [JsonIgnore]
    public string CpuName => CpuBrand;

    [JsonPropertyName("cpuCores")]
    public uint CpuCores { get; init; }

    [JsonPropertyName("totalMemory")]
    public ulong TotalMemory { get; init; }

    [JsonIgnore]
    public long TotalMemoryMB => (long)(TotalMemory / (1024UL * 1024UL));

    [JsonPropertyName("disks")]
    public List<DiskInfoData> Disks { get; init; } = new();

    [JsonPropertyName("networkAdapters")]
    public List<NetworkAdapterData> NetworkAdapters { get; init; } = new();

    [JsonPropertyName("usbDevices")]
    public List<UsbDeviceData> UsbDevices { get; init; } = new();

    [JsonPropertyName("usbControllers")]
    public List<UsbControllerData> UsbControllers { get; init; } = new();

    [JsonPropertyName("gpus")]
    public List<GpuInfoData> Gpus { get; init; } = new();
}

/// <summary>
/// Disk information
/// </summary>
public record DiskInfoData
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("fileSystem")]
    public string FileSystem { get; init; } = string.Empty;

    [JsonPropertyName("totalSpace")]
    public ulong TotalSpace { get; init; }

    [JsonPropertyName("availableSpace")]
    public ulong AvailableSpace { get; init; }

    [JsonIgnore]
    public long TotalSizeGB => (long)(TotalSpace / (1024UL * 1024UL * 1024UL));

    [JsonIgnore]
    public long FreeSpaceGB => (long)(AvailableSpace / (1024UL * 1024UL * 1024UL));

    [JsonIgnore]
    public double UsagePercentage => TotalSpace > 0
        ? (double)(TotalSpace - AvailableSpace) / TotalSpace * 100
        : 0;
}

/// <summary>
/// Network adapter information
/// </summary>
public record NetworkAdapterData
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("macAddress")]
    public string MacAddress { get; init; } = string.Empty;

    [JsonPropertyName("ipAddresses")]
    public List<string> IpAddresses { get; init; } = new();

    [JsonIgnore]
    public string IpAddress => IpAddresses.Count > 0 ? IpAddresses[0] : string.Empty;
}

/// <summary>
/// USB device information
/// </summary>
public record UsbDeviceData
{
    [JsonPropertyName("deviceId")]
    public string DeviceId { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("manufacturer")]
    public string? Manufacturer { get; init; }

    [JsonPropertyName("pnpDeviceId")]
    public string? PnpDeviceId { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }
}

/// <summary>
/// USB controller information
/// </summary>
public record UsbControllerData
{
    [JsonPropertyName("deviceId")]
    public string DeviceId { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("manufacturer")]
    public string? Manufacturer { get; init; }

    [JsonPropertyName("caption")]
    public string? Caption { get; init; }

    [JsonPropertyName("protocolVersion")]
    public string? ProtocolVersion { get; init; }

    [JsonIgnore]
    public string Protocol => ProtocolVersion ?? string.Empty;
}

/// <summary>
/// GPU information
/// </summary>
public record GpuInfoData
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("driverVersion")]
    public string? DriverVersion { get; init; }

    [JsonPropertyName("videoMemory")]
    public ulong? VideoMemory { get; init; }

    [JsonPropertyName("temperature")]
    public float Temperature { get; init; }

    [JsonPropertyName("load")]
    public float Load { get; init; }

    [JsonPropertyName("memoryUsed")]
    public float MemoryUsed { get; init; }

    [JsonPropertyName("memoryTotal")]
    public float MemoryTotal { get; init; }

    [JsonPropertyName("coreClock")]
    public float CoreClock { get; init; }

    [JsonPropertyName("memoryClock")]
    public float MemoryClock { get; init; }

    [JsonPropertyName("fanSpeed")]
    public float FanSpeed { get; init; }

    [JsonPropertyName("power")]
    public float Power { get; init; }
}

/// <summary>
/// Windows reliability record
/// </summary>
public record ReliabilityRecordData
{
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; }

    [JsonPropertyName("sourceName")]
    public string SourceName { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("eventType")]
    public string EventType { get; init; } = string.Empty;

    [JsonPropertyName("recordType")]
    public int? RecordType { get; init; }

    [JsonIgnore]
    public DateTime TimeGenerated => Timestamp;

    [JsonIgnore]
    public string ProductName => SourceName;

    [JsonIgnore]
    public string RecordTypeDescription => RecordType switch
    {
        1 => "应用程序故障",
        2 => "Windows 故障",
        3 => "其他故障",
        null => string.IsNullOrWhiteSpace(EventType) ? "未知" : EventType,
        _ => "未知"
    };
}

/// <summary>
/// Windows event log entry
/// </summary>
public record LogEventData
{
    [JsonPropertyName("timeGenerated")]
    public DateTime TimeGenerated { get; init; }

    [JsonPropertyName("logFile")]
    public string LogFile { get; init; } = string.Empty;

    [JsonPropertyName("sourceName")]
    public string SourceName { get; init; } = string.Empty;

    [JsonIgnore]
    public string Source => SourceName;

    [JsonPropertyName("eventType")]
    public string EventType { get; init; } = string.Empty;

    [JsonPropertyName("eventId")]
    public uint EventId { get; init; }

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [JsonIgnore]
    public string LogName => LogFile;
}

/// <summary>
/// Performance analysis results
/// </summary>
public record PerformanceAnalysisData
{
    [JsonPropertyName("systemHealthScore")]
    public double SystemHealthScore { get; init; }

    [JsonPropertyName("stabilityScore")]
    public double StabilityScore { get; init; }

    [JsonPropertyName("performanceScore")]
    public double PerformanceScore { get; init; }

    [JsonPropertyName("memoryUsageScore")]
    public double MemoryUsageScore { get; init; }

    [JsonPropertyName("diskHealthScore")]
    public double DiskHealthScore { get; init; }

    [JsonPropertyName("systemUptimeDays")]
    public double? SystemUptimeDays { get; init; }

    [JsonPropertyName("criticalIssuesCount")]
    public uint CriticalIssuesCount { get; init; }

    [JsonPropertyName("warningsCount")]
    public uint WarningsCount { get; init; }

    [JsonPropertyName("recommendations")]
    public List<string> Recommendations { get; init; } = new();

    [JsonPropertyName("healthGrade")]
    public string HealthGrade { get; init; } = string.Empty;

    [JsonPropertyName("healthColor")]
    public string HealthColor { get; init; } = string.Empty;
}
