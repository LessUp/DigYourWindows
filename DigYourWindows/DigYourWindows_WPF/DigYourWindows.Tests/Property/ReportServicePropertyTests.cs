using DigYourWindows.Core.Models;
using DigYourWindows.Core.Services;

namespace DigYourWindows.Tests.Property;

public class ReportServicePropertyTests
{
    [PropertyTest]
    public void SerializeDeserialize_RoundTripPreservesHardwareCoreFields(
        NonEmptyString computerName,
        NonEmptyString osVersion,
        NonEmptyString cpuBrand,
        NonNegativeInt cpuCoresRaw,
        NonNegativeInt memoryGBRaw)
    {
        var cpuCores = (uint)(cpuCoresRaw.Get % 128);
        var memoryGB = (ulong)(memoryGBRaw.Get % 128);
        var totalMemory = memoryGB * 1024UL * 1024UL * 1024UL;

        var collectedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var data = new DiagnosticData
        {
            Hardware = new HardwareData
            {
                ComputerName = computerName.Get,
                OsVersion = osVersion.Get,
                CpuBrand = cpuBrand.Get,
                CpuCores = cpuCores,
                TotalMemory = totalMemory
            },
            CollectedAt = collectedAt
        };

        var service = new ReportService();
        var json = service.SerializeToJson(data, indented: false);
        var deserialized = service.DeserializeFromJson(json);

        Assert.NotNull(deserialized);
        Assert.Equal(data.Hardware.ComputerName, deserialized!.Hardware.ComputerName);
        Assert.Equal(data.Hardware.OsVersion, deserialized.Hardware.OsVersion);
        Assert.Equal(data.Hardware.CpuBrand, deserialized.Hardware.CpuBrand);
        Assert.Equal(data.Hardware.CpuCores, deserialized.Hardware.CpuCores);
        Assert.Equal(data.Hardware.TotalMemory, deserialized.Hardware.TotalMemory);
        Assert.Equal(collectedAt, deserialized.CollectedAt);
    }
}
