using System.Management;
using DigYourWindows.Core.Models;

namespace DigYourWindows.Core.Services;

public class HardwareService
{
    private readonly GpuMonitorService _gpuMonitor;

    public HardwareService()
    {
        _gpuMonitor = new GpuMonitorService();
    }

    public HardwareInfo GetHardwareInfo()
    {
        return new HardwareInfo
        {
            ComputerName = Environment.MachineName,
            OsVersion = Environment.OSVersion.ToString(),
            CpuName = GetCpuName(),
            CpuCores = Environment.ProcessorCount,
            TotalMemoryMB = GetTotalMemoryMB(),
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

    private long GetTotalMemoryMB()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
            foreach (var obj in searcher.Get())
            {
                var bytes = Convert.ToInt64(obj["TotalPhysicalMemory"]);
                return bytes / (1024 * 1024);
            }
        }
        catch { }
        return 0;
    }

    private List<DiskInfo> GetDisks()
    {
        var disks = new List<DiskInfo>();
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDisk WHERE DriveType = 3");
            foreach (ManagementObject obj in searcher.Get())
            {
                var size = Convert.ToInt64(obj["Size"] ?? 0);
                var freeSpace = Convert.ToInt64(obj["FreeSpace"] ?? 0);
                
                disks.Add(new DiskInfo
                {
                    Name = obj["Name"]?.ToString() ?? "",
                    FileSystem = obj["FileSystem"]?.ToString() ?? "",
                    TotalSizeGB = size / (1024 * 1024 * 1024),
                    FreeSpaceGB = freeSpace / (1024 * 1024 * 1024)
                });
            }
        }
        catch { }
        return disks;
    }

    private List<NetworkAdapterInfo> GetNetworkAdapters()
    {
        var adapters = new List<NetworkAdapterInfo>();
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT * FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = True");
            
            foreach (ManagementObject obj in searcher.Get())
            {
                var ipAddresses = obj["IPAddress"] as string[];
                adapters.Add(new NetworkAdapterInfo
                {
                    Name = obj["Description"]?.ToString() ?? "",
                    MacAddress = obj["MACAddress"]?.ToString() ?? "",
                    IpAddress = ipAddresses?.FirstOrDefault() ?? "",
                    Status = "已连接"
                });
            }
        }
        catch { }
        return adapters;
    }

    private List<UsbDeviceInfo> GetUsbDevices()
    {
        var devices = new List<UsbDeviceInfo>();
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT DeviceID, Name, Description, Manufacturer FROM Win32_PnPEntity WHERE DeviceID LIKE 'USB%'");
            
            foreach (ManagementObject obj in searcher.Get())
            {
                devices.Add(new UsbDeviceInfo
                {
                    DeviceId = obj["DeviceID"]?.ToString() ?? "",
                    Name = obj["Name"]?.ToString() ?? "",
                    Description = obj["Description"]?.ToString() ?? "",
                    Manufacturer = obj["Manufacturer"]?.ToString() ?? ""
                });
            }
        }
        catch { }
        return devices;
    }

    private List<UsbControllerInfo> GetUsbControllers()
    {
        var controllers = new List<UsbControllerInfo>();
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name, Manufacturer, Caption FROM Win32_USBController");
            
            foreach (ManagementObject obj in searcher.Get())
            {
                var caption = obj["Caption"]?.ToString() ?? "";
                var protocol = caption.Contains("3.0") || caption.Contains("3.1") || caption.Contains("xHCI") 
                    ? "USB 3.x" 
                    : "USB 2.0";
                
                controllers.Add(new UsbControllerInfo
                {
                    Name = obj["Name"]?.ToString() ?? "",
                    Manufacturer = obj["Manufacturer"]?.ToString() ?? "",
                    Protocol = protocol
                });
            }
        }
        catch { }
        return controllers;
    }
}
