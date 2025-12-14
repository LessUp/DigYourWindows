using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DigYourWindows.Core.Models;
using DigYourWindows.Core.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using Wpf.Ui.Appearance;

namespace DigYourWindows.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly HardwareService _hardwareService;
    private readonly ReliabilityService _reliabilityService;
    private readonly EventLogService _eventLogService;
    private readonly PerformanceService _performanceService;
    private bool _reloadRequested;

    [ObservableProperty]
    private HardwareInfo? _hardwareInfo;

    [ObservableProperty]
    private ObservableCollection<ReliabilityRecord> _reliabilityRecords = new();

    [ObservableProperty]
    private ObservableCollection<DigYourWindows.Core.Models.EventLogEntry> _eventLogEntries = new();

    [ObservableProperty]
    private PerformanceAnalysis? _performanceAnalysis;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "就绪";

    [ObservableProperty]
    private int _selectedDaysBack = 3;

    [ObservableProperty]
    private ApplicationTheme _currentTheme = ApplicationTheme.Dark;

    public List<int> AvailableDays { get; } = new() { 1, 3, 7, 30 };

    public MainViewModel(
        HardwareService hardwareService,
        ReliabilityService reliabilityService,
        EventLogService eventLogService,
        PerformanceService performanceService)
    {
        _hardwareService = hardwareService;
        _reliabilityService = reliabilityService;
        _eventLogService = eventLogService;
        _performanceService = performanceService;
    }

    partial void OnSelectedDaysBackChanged(int value)
    {
        if (IsLoading)
        {
            _reloadRequested = true;
            return;
        }

        _ = LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        StatusMessage = "正在加载数据...";

        try
        {
            StatusMessage = "正在获取硬件信息...";
            var hardware = await Task.Run(() => _hardwareService.GetHardwareInfo());

            StatusMessage = "正在获取可靠性记录...";
            var reliability = await Task.Run(() => _reliabilityService.GetReliabilityRecords(7));

            var daysBack = SelectedDaysBack;
            StatusMessage = $"正在获取事件日志 (最近{daysBack}天)...";
            var events = await Task.Run(() => _eventLogService.GetErrorEvents(daysBack));

            StatusMessage = "正在进行性能分析...";
            var analysis = await Task.Run(() =>
                _performanceService.AnalyzeSystemPerformance(hardware, events, reliability));

            HardwareInfo = hardware;

            ReliabilityRecords.Clear();
            foreach (var record in reliability)
            {
                ReliabilityRecords.Add(record);
            }

            EventLogEntries.Clear();
            foreach (var evt in events)
            {
                EventLogEntries.Add(evt);
            }

            PerformanceAnalysis = analysis;

            var performanceScore = analysis.SystemHealthScore;
            StatusMessage = $"数据加载完成 | 可靠性记录: {ReliabilityRecords.Count} | 错误事件: {EventLogEntries.Count} | 系统健康评分: {performanceScore:F0}/100";
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;

            if (_reloadRequested)
            {
                _reloadRequested = false;
                _ = LoadDataAsync();
            }
        }
    }

    [RelayCommand]
    private async Task ExportToHtmlAsync()
    {
        try
        {
            StatusMessage = "正在导出HTML报告...";
            IsLoading = true;

            var html = GenerateHtmlReport();
            var fileName = $"DigYourWindows_Report_{DateTime.Now:yyyyMMdd_HHmmss}.html";
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

            await Task.Run(() => File.WriteAllText(filePath, html, Encoding.UTF8));

            StatusMessage = $"报告已导出: {fileName}";
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            StatusMessage = $"导出失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        CurrentTheme = CurrentTheme == ApplicationTheme.Dark 
            ? ApplicationTheme.Light 
            : ApplicationTheme.Dark;
        
        ApplicationThemeManager.Apply(CurrentTheme);
        StatusMessage = $"主题已切换为: {(CurrentTheme == ApplicationTheme.Dark ? "深色" : "浅色")}";
    }

    private string GenerateHtmlReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang='zh-CN'>");
        sb.AppendLine("<head>");
        sb.AppendLine("    <meta charset='UTF-8'>");
        sb.AppendLine("    <meta name='viewport' content='width=device-width, initial-scale=1.0'>");
        sb.AppendLine("    <title>DigYourWindows 诊断报告</title>");
        sb.AppendLine("    <link href='https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css' rel='stylesheet'>");
        sb.AppendLine("    <style>");
        sb.AppendLine("        body { padding: 20px; background: #f5f5f5; }");
        sb.AppendLine("        .card { margin-bottom: 20px; }");
        sb.AppendLine("        .metric { font-size: 1.5rem; font-weight: bold; }");
        sb.AppendLine("    </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine($"    <h1 class='mb-4'>Windows 诊断报告 - {DateTime.Now:yyyy-MM-dd HH:mm:ss}</h1>");
        
        // 系统概览
        sb.AppendLine("    <div class='card'>");
        sb.AppendLine("        <div class='card-header'><h3>系统概览</h3></div>");
        sb.AppendLine("        <div class='card-body'>");
        sb.AppendLine("            <div class='row'>");
        sb.AppendLine($"                <div class='col-md-3'><strong>计算机名:</strong> {HardwareInfo?.ComputerName}</div>");
        sb.AppendLine($"                <div class='col-md-3'><strong>操作系统:</strong> {HardwareInfo?.OsVersion}</div>");
        sb.AppendLine($"                <div class='col-md-3'><strong>CPU:</strong> {HardwareInfo?.CpuName}</div>");
        sb.AppendLine($"                <div class='col-md-3'><strong>内存:</strong> {HardwareInfo?.TotalMemoryMB} MB</div>");
        sb.AppendLine("            </div>");
        sb.AppendLine("        </div>");
        sb.AppendLine("    </div>");

        // 性能分析
        if (PerformanceAnalysis != null)
        {
            sb.AppendLine("    <div class='card'>");
            sb.AppendLine("        <div class='card-header'><h3>系统性能分析</h3></div>");
            sb.AppendLine("        <div class='card-body'>");
            sb.AppendLine("            <div class='row mb-3'>");
            sb.AppendLine("                <div class='col-md-3'>");
            sb.AppendLine("                    <div class='card text-center p-3'>");
            sb.AppendLine("                        <h5>系统健康评分</h5>");
            sb.AppendLine($"                        <div class='metric' style='color: {PerformanceAnalysis.HealthColor}'>{PerformanceAnalysis.SystemHealthScore:F0}/100</div>");
            sb.AppendLine($"                        <span class='badge bg-secondary'>{PerformanceAnalysis.HealthGrade}</span>");
            sb.AppendLine("                    </div>");
            sb.AppendLine("                </div>");
            sb.AppendLine("                <div class='col-md-3'>");
            sb.AppendLine("                    <div class='card text-center p-3'>");
            sb.AppendLine("                        <h5>稳定性评分</h5>");
            sb.AppendLine($"                        <div class='metric'>{PerformanceAnalysis.StabilityScore:F0}/100</div>");
            sb.AppendLine("                    </div>");
            sb.AppendLine("                </div>");
            sb.AppendLine("                <div class='col-md-3'>");
            sb.AppendLine("                    <div class='card text-center p-3'>");
            sb.AppendLine("                        <h5>性能评分</h5>");
            sb.AppendLine($"                        <div class='metric'>{PerformanceAnalysis.PerformanceScore:F0}/100</div>");
            sb.AppendLine("                    </div>");
            sb.AppendLine("                </div>");
            sb.AppendLine("                <div class='col-md-3'>");
            sb.AppendLine("                    <div class='card text-center p-3'>");
            sb.AppendLine("                        <h5>内存评分</h5>");
            sb.AppendLine($"                        <div class='metric'>{PerformanceAnalysis.MemoryUsageScore:F0}/100</div>");
            sb.AppendLine("                    </div>");
            sb.AppendLine("                </div>");
            sb.AppendLine("            </div>");
            sb.AppendLine("            <div class='row'>");
            sb.AppendLine("                <div class='col-md-3'>");
            sb.AppendLine("                    <div class='card text-center p-3'>");
            sb.AppendLine("                        <h5>磁盘健康</h5>");
            sb.AppendLine($"                        <div class='metric'>{PerformanceAnalysis.DiskHealthScore:F0}/100</div>");
            sb.AppendLine("                    </div>");
            sb.AppendLine("                </div>");
            sb.AppendLine("                <div class='col-md-3'>");
            sb.AppendLine("                    <div class='card text-center p-3'>");
            sb.AppendLine("                        <h5>关键问题</h5>");
            sb.AppendLine($"                        <div class='metric text-danger'>{PerformanceAnalysis.CriticalIssuesCount}</div>");
            sb.AppendLine("                    </div>");
            sb.AppendLine("                </div>");
            sb.AppendLine("                <div class='col-md-3'>");
            sb.AppendLine("                    <div class='card text-center p-3'>");
            sb.AppendLine("                        <h5>警告数量</h5>");
            sb.AppendLine($"                        <div class='metric text-warning'>{PerformanceAnalysis.WarningsCount}</div>");
            sb.AppendLine("                    </div>");
            sb.AppendLine("                </div>");
            sb.AppendLine("                <div class='col-md-3'>");
            sb.AppendLine("                    <div class='card text-center p-3'>");
            sb.AppendLine("                        <h5>系统运行时间</h5>");
            sb.AppendLine($"                        <div class='metric'>{PerformanceAnalysis.SystemUptimeDays:F0} 天</div>");
            sb.AppendLine("                    </div>");
            sb.AppendLine("                </div>");
            sb.AppendLine("            </div>");

            // 优化建议
            if (PerformanceAnalysis.Recommendations.Any())
            {
                sb.AppendLine("            <div class='mt-4'>");
                sb.AppendLine("                <h5>优化建议</h5>");
                sb.AppendLine("                <ul>");
                foreach (var recommendation in PerformanceAnalysis.Recommendations)
                {
                    sb.AppendLine($"                    <li>{recommendation}</li>");
                }
                sb.AppendLine("                </ul>");
                sb.AppendLine("            </div>");
            }

            sb.AppendLine("        </div>");
            sb.AppendLine("    </div>");
        }

        // GPU信息
        if (HardwareInfo?.Gpus?.Count > 0)
        {
            sb.AppendLine("    <div class='card'>");
            sb.AppendLine("        <div class='card-header'><h3>GPU 信息</h3></div>");
            sb.AppendLine("        <div class='card-body'>");
            sb.AppendLine("            <table class='table'>");
            sb.AppendLine("                <thead><tr><th>名称</th><th>温度</th><th>负载</th><th>显存</th><th>核心频率</th><th>功耗</th></tr></thead>");
            sb.AppendLine("                <tbody>");
            foreach (var gpu in HardwareInfo.Gpus)
            {
                sb.AppendLine($"                    <tr><td>{gpu.Name}</td><td>{gpu.Temperature:F1}°C</td><td>{gpu.Load:F1}%</td><td>{gpu.MemoryUsed:F0}/{gpu.MemoryTotal:F0} MB</td><td>{gpu.CoreClock:F0} MHz</td><td>{gpu.Power:F1} W</td></tr>");
            }
            sb.AppendLine("                </tbody>");
            sb.AppendLine("            </table>");
            sb.AppendLine("        </div>");
            sb.AppendLine("    </div>");
        }

        // 事件日志
        sb.AppendLine("    <div class='card'>");
        sb.AppendLine($"        <div class='card-header'><h3>错误日志 (最近{SelectedDaysBack}天) - {EventLogEntries.Count} 条</h3></div>");
        sb.AppendLine("        <div class='card-body'>");
        sb.AppendLine("            <table class='table table-sm table-striped'>");
        sb.AppendLine("                <thead><tr><th>时间</th><th>来源</th><th>类型</th><th>ID</th><th>消息</th></tr></thead>");
        sb.AppendLine("                <tbody>");
        foreach (var evt in EventLogEntries.Take(100))
        {
            sb.AppendLine($"                    <tr><td>{evt.TimeGenerated:yyyy-MM-dd HH:mm}</td><td>{evt.Source}</td><td>{evt.EventType}</td><td>{evt.EventId}</td><td>{evt.Message?.Substring(0, Math.Min(evt.Message.Length, 100))}</td></tr>");
        }
        sb.AppendLine("                </tbody>");
        sb.AppendLine("            </table>");
        sb.AppendLine("        </div>");
        sb.AppendLine("    </div>");
        
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        
        return sb.ToString();
    }
}
