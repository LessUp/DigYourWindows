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

    [JsonPropertyName("cpuCores")]
    public uint CpuCores { get; init; }

    [JsonPropertyName("totalMemory")]
    public ulong TotalMemory { get; init; }

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

    [JsonPropertyName("eventType")]
    public string EventType { get; init; } = string.Empty;

    [JsonPropertyName("eventId")]
    public uint EventId { get; init; }

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;
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
