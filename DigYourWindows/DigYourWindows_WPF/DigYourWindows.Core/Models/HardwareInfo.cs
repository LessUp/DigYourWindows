namespace DigYourWindows.Core.Models;

public record HardwareInfo
{
    public string ComputerName { get; init; } = string.Empty;
    public string OsVersion { get; init; } = string.Empty;
    public string CpuName { get; init; } = string.Empty;
    public int CpuCores { get; init; }
    public long TotalMemoryMB { get; init; }
    public List<DiskInfo> Disks { get; init; } = new();
    public List<NetworkAdapterInfo> NetworkAdapters { get; init; } = new();
    public List<UsbDeviceInfo> UsbDevices { get; init; } = new();
    public List<UsbControllerInfo> UsbControllers { get; init; } = new();
}

public record DiskInfo
{
    public string Name { get; init; } = string.Empty;
    public string FileSystem { get; init; } = string.Empty;
    public long TotalSizeGB { get; init; }
    public long FreeSpaceGB { get; init; }
    public double UsagePercentage => TotalSizeGB > 0 ? (double)(TotalSizeGB - FreeSpaceGB) / TotalSizeGB * 100 : 0;
}

public record NetworkAdapterInfo
{
    public string Name { get; init; } = string.Empty;
    public string MacAddress { get; init; } = string.Empty;
    public string IpAddress { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}

public record UsbDeviceInfo
{
    public string DeviceId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Manufacturer { get; init; } = string.Empty;
}

public record UsbControllerInfo
{
    public string Name { get; init; } = string.Empty;
    public string Manufacturer { get; init; } = string.Empty;
    public string Protocol { get; init; } = string.Empty;
}
