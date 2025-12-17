using DigYourWindows.Core.Models;

namespace DigYourWindows.Core.Services;

public class DiagnosticCollectorService
{
    private readonly HardwareService _hardwareService;
    private readonly ReliabilityService _reliabilityService;
    private readonly EventLogService _eventLogService;
    private readonly PerformanceService _performanceService;
    private readonly ILogService _log;

    public DiagnosticCollectorService(
        HardwareService hardwareService,
        ReliabilityService reliabilityService,
        EventLogService eventLogService,
        PerformanceService performanceService,
        ILogService log)
    {
        _hardwareService = hardwareService;
        _reliabilityService = reliabilityService;
        _eventLogService = eventLogService;
        _performanceService = performanceService;
        _log = log;
    }

    public async Task<DiagnosticCollectionResult> CollectAsync(
        int daysBack,
        IProgress<DiagnosticCollectionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var warnings = new List<string>();
        const int stepCount = 4;

        progress?.Report(new DiagnosticCollectionProgress(1, stepCount, "正在获取硬件信息..."));
        HardwareData hardware;
        try
        {
            hardware = await Task.Run(() => _hardwareService.GetHardwareInfo(), cancellationToken);
        }
        catch (Exception ex)
        {
            _log.Error("获取硬件信息失败", ex);
            warnings.Add($"硬件信息获取失败: {ex.Message}");
            hardware = new HardwareData();
        }

        progress?.Report(new DiagnosticCollectionProgress(2, stepCount, "正在获取可靠性记录..."));
        List<ReliabilityRecordData> reliability;
        try
        {
            var reliabilityRaw = await Task.Run(() => _reliabilityService.GetReliabilityRecords(7), cancellationToken);
            reliability = reliabilityRaw.ToList();
        }
        catch (Exception ex)
        {
            _log.Error("获取可靠性记录失败", ex);
            warnings.Add($"可靠性记录获取失败: {ex.Message}");
            reliability = new List<ReliabilityRecordData>();
        }

        progress?.Report(new DiagnosticCollectionProgress(3, stepCount, $"正在获取事件日志 (最近{daysBack}天)..."));
        List<LogEventData> events;
        try
        {
            events = await Task.Run(() => _eventLogService.GetErrorEvents(daysBack), cancellationToken);
        }
        catch (Exception ex)
        {
            _log.Error("获取事件日志失败", ex);
            warnings.Add($"事件日志获取失败: {ex.Message}");
            events = new List<LogEventData>();
        }

        progress?.Report(new DiagnosticCollectionProgress(4, stepCount, "正在进行性能分析..."));
        PerformanceAnalysisData analysis;
        try
        {
            analysis = await Task.Run(
                () => _performanceService.AnalyzeSystemPerformance(hardware, events, reliability),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _log.Error("性能分析失败", ex);
            warnings.Add($"性能分析失败: {ex.Message}");
            analysis = new PerformanceAnalysisData();
        }

        var data = new DiagnosticData
        {
            Hardware = hardware,
            Reliability = reliability,
            Events = events,
            Performance = analysis,
            CollectedAt = DateTime.UtcNow
        };

        return new DiagnosticCollectionResult(data, warnings);
    }
}
