using System.Management;
using DigYourWindows.Core.Models;

namespace DigYourWindows.Core.Services;

public class DiskSmartService
{
    public List<DiskSmartData> GetDiskSmart()
    {
        var result = new List<DiskSmartData>();

        try
        {
            var scope = new ManagementScope("\\\\.\\root\\Microsoft\\Windows\\Storage");
            scope.Connect();

            var reliability = new Dictionary<string, (ushort? Temperature, ushort? Wear, uint? PowerOnHours)>(StringComparer.OrdinalIgnoreCase);

            using (var reliabilitySearcher = new ManagementObjectSearcher(
                       scope,
                       new ObjectQuery("SELECT DeviceId, Temperature, Wear, PowerOnHours FROM MSFT_StorageReliabilityCounter")))
            {
                foreach (ManagementObject counter in reliabilitySearcher.Get())
                {
                    var deviceId = counter["DeviceId"]?.ToString();
                    if (string.IsNullOrWhiteSpace(deviceId))
                    {
                        continue;
                    }

                    reliability[deviceId] = (
                        Temperature: TryGetUShort(counter["Temperature"]),
                        Wear: TryGetUShort(counter["Wear"]),
                        PowerOnHours: TryGetUInt(counter["PowerOnHours"]));
                }
            }

            using (var diskSearcher = new ManagementObjectSearcher(
                       scope,
                       new ObjectQuery("SELECT DeviceId, FriendlyName, SerialNumber, BusType, MediaType, Size, HealthStatus FROM MSFT_PhysicalDisk")))
            {
                foreach (ManagementObject disk in diskSearcher.Get())
                {
                    var deviceId = disk["DeviceId"]?.ToString() ?? string.Empty;
                    reliability.TryGetValue(deviceId, out var rel);

                    result.Add(new DiskSmartData
                    {
                        DeviceId = deviceId,
                        FriendlyName = disk["FriendlyName"]?.ToString() ?? string.Empty,
                        SerialNumber = disk["SerialNumber"]?.ToString(),
                        BusType = TryGetUShort(disk["BusType"]),
                        MediaType = TryGetUShort(disk["MediaType"]),
                        Size = TryGetULong(disk["Size"]),
                        HealthStatus = TryGetUShort(disk["HealthStatus"]),
                        Temperature = rel.Temperature,
                        Wear = rel.Wear,
                        PowerOnHours = rel.PowerOnHours
                    });
                }
            }
        }
        catch
        {
        }

        return result;
    }

    private static ushort? TryGetUShort(object? value)
    {
        if (value is null)
        {
            return null;
        }

        try
        {
            return Convert.ToUInt16(value);
        }
        catch
        {
            return null;
        }
    }

    private static uint? TryGetUInt(object? value)
    {
        if (value is null)
        {
            return null;
        }

        try
        {
            return Convert.ToUInt32(value);
        }
        catch
        {
            return null;
        }
    }

    private static ulong TryGetULong(object? value)
    {
        if (value is null)
        {
            return 0UL;
        }

        try
        {
            return Convert.ToUInt64(value);
        }
        catch
        {
            return 0UL;
        }
    }
}
