using DigYourWindows.Core.Models;
using LibreHardwareMonitor.Hardware;

namespace DigYourWindows.Core.Services;

public class CpuMonitorService : IDisposable
{
    private readonly Computer _computer;
    private bool _disposed;

    public CpuMonitorService()
    {
        _computer = new Computer
        {
            IsCpuEnabled = true
        };
        _computer.Open();
    }

    public CpuInfoData GetCpuInfo()
    {
        try
        {
            var cpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
            if (cpu is null)
            {
                return new CpuInfoData();
            }

            cpu.Update();

            var sensors = GetAllSensors(cpu).ToList();

            var temperature = GetPreferredSensorValue(sensors, SensorType.Temperature, new[]
            {
                "CPU Package",
                "Core (Tctl/Tdie)",
                "Tctl/Tdie",
                "Package",
                "CPU Core",
                "Core"
            });

            var load = GetPreferredSensorValue(sensors, SensorType.Load, new[]
            {
                "CPU Total",
                "Total",
                "CPU"
            });

            var clock = GetClockMHz(sensors);

            return new CpuInfoData
            {
                Name = cpu.Name,
                Temperature = temperature,
                Load = load,
                Clock = clock
            };
        }
        catch
        {
            return new CpuInfoData();
        }
    }

    private static IEnumerable<ISensor> GetAllSensors(IHardware hardware)
    {
        foreach (var sensor in hardware.Sensors)
        {
            yield return sensor;
        }

        foreach (var sub in hardware.SubHardware)
        {
            sub.Update();
            foreach (var sensor in GetAllSensors(sub))
            {
                yield return sensor;
            }
        }
    }

    private static float GetPreferredSensorValue(List<ISensor> sensors, SensorType sensorType, string[] preferredNames)
    {
        var candidates = sensors.Where(s => s.SensorType == sensorType).ToList();

        foreach (var preferred in preferredNames)
        {
            var hit = candidates.FirstOrDefault(s =>
                s.Value.HasValue &&
                s.Name.Contains(preferred, StringComparison.OrdinalIgnoreCase));

            if (hit?.Value is not null)
            {
                return hit.Value.Value;
            }
        }

        return candidates.FirstOrDefault(s => s.Value.HasValue)?.Value ?? 0f;
    }

    private static float GetClockMHz(List<ISensor> sensors)
    {
        var clocks = sensors
            .Where(s => s.SensorType == SensorType.Clock && s.Value.HasValue)
            .ToList();

        var coreClocks = clocks
            .Where(s =>
                s.Name.Contains("Core", StringComparison.OrdinalIgnoreCase) &&
                !s.Name.Contains("Bus", StringComparison.OrdinalIgnoreCase))
            .Select(s => s.Value!.Value)
            .ToList();

        if (coreClocks.Count > 0)
        {
            return (float)coreClocks.Average();
        }

        return clocks.FirstOrDefault()?.Value ?? 0f;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _computer?.Close();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
