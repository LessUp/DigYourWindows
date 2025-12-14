using System.Management;
using DigYourWindows.Core.Models;

namespace DigYourWindows.Core.Services;

public class HardwareService
{
    private readonly GpuMonitorService _gpuMonitor;

    public HardwareService(GpuMonitorService gpuMonitor)
    {
        _gpuMonitor = gpuMonitor;
    }

    public HardwareData GetHardwareInfo()
    {
        return new HardwareData
        {
            ComputerName = Environment.MachineName,
            OsVersion = Environment.OSVersion.ToString(),
            CpuBrand = GetCpuName(),
            CpuCores = (uint)Environment.ProcessorCount,
            TotalMemory = GetTotalMemoryBytes(),
            Disks = GetDisks(),
            NetworkAdapters = GetNetworkAdapters(),
            UsbDevices = GetUsbDevices(),
            UsbControllers = GetUsbControllers(),
            Gpus = _gpuMonitor.GetGpuInfo()
        };
    }

    private string GetCpuName()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor");
            foreach (var obj in searcher.Get())
            {
                return obj["Name"]?.ToString()?.Trim() ?? "Unknown";
            }
        }
        catch { }
        return "Unknown CPU";
    }

    private ulong GetTotalMemoryBytes()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
            foreach (var obj in searcher.Get())
            {
                return Convert.ToUInt64(obj["TotalPhysicalMemory"] ?? 0UL);
            }
        }
        catch { }
        return 0UL;
    }

    private List<DiskInfoData> GetDisks()
    {
        var disks = new List<DiskInfoData>();
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name, FileSystem, Size, FreeSpace FROM Win32_LogicalDisk WHERE DriveType = 3");
            foreach (ManagementObject obj in searcher.Get())
            {
                var size = Convert.ToUInt64(obj["Size"] ?? 0UL);
                var freeSpace = Convert.ToUInt64(obj["FreeSpace"] ?? 0UL);

                disks.Add(new DiskInfoData
                {
                    Name = obj["Name"]?.ToString() ?? "",
                    FileSystem = obj["FileSystem"]?.ToString() ?? "",
                    TotalSpace = size,
                    AvailableSpace = freeSpace
                });
            }
        }
        catch { }
        return disks;
    }

    private List<NetworkAdapterData> GetNetworkAdapters()
    {
        var adapters = new List<NetworkAdapterData>();
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT * FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = True");

            foreach (ManagementObject obj in searcher.Get())
            {
                var ipAddresses = obj["IPAddress"] as string[];
                adapters.Add(new NetworkAdapterData
                {
                    Name = obj["Description"]?.ToString() ?? "",
                    MacAddress = obj["MACAddress"]?.ToString() ?? "",
                    IpAddresses = ipAddresses?.ToList() ?? new List<string>()
                });
            }
        }
        catch { }
        return adapters;
    }

    private List<UsbDeviceData> GetUsbDevices()
    {
        var devices = new List<UsbDeviceData>();
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT DeviceID, Name, Description, Manufacturer FROM Win32_PnPEntity WHERE DeviceID LIKE 'USB%'");

            foreach (ManagementObject obj in searcher.Get())
            {
                devices.Add(new UsbDeviceData
                {
                    DeviceId = obj["DeviceID"]?.ToString() ?? "",
                    Name = obj["Name"]?.ToString(),
                    Description = obj["Description"]?.ToString(),
                    Manufacturer = obj["Manufacturer"]?.ToString()
                });
            }
        }
        catch { }
        return devices;
    }

    private List<UsbControllerData> GetUsbControllers()
    {
        var controllers = new List<UsbControllerData>();
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT DeviceID, Name, Manufacturer, Caption FROM Win32_USBController");

            foreach (ManagementObject obj in searcher.Get())
            {
                var caption = obj["Caption"]?.ToString() ?? "";
                var protocol = caption.Contains("3.0") || caption.Contains("3.1") || caption.Contains("xHCI") 
                    ? "USB 3.x" 
                    : "USB 2.0";

                var deviceId = obj["DeviceID"]?.ToString();
                var name = obj["Name"]?.ToString();

                controllers.Add(new UsbControllerData
                {
                    DeviceId = string.IsNullOrWhiteSpace(deviceId) ? (name ?? string.Empty) : deviceId,
                    Name = name,
                    Manufacturer = obj["Manufacturer"]?.ToString(),
                    Caption = caption,
                    ProtocolVersion = protocol
                });
            }
        }
        catch { }
        return controllers;
    }
}
