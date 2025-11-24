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

    [ObservableProperty]
    private HardwareInfo? _hardwareInfo;

    [ObservableProperty]
    private ObservableCollection<ReliabilityRecord> _reliabilityRecords = new();

    [ObservableProperty]
    private ObservableCollection<DigYourWindows.Core.Models.EventLogEntry> _eventLogEntries = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "就绪";

    [ObservableProperty]
    private int _selectedDaysBack = 3;

    [ObservableProperty]
    private ApplicationTheme _currentTheme = ApplicationTheme.Dark;

    public List<int> AvailableDays { get; } = new() { 1, 3, 7, 30 };

    public MainViewModel()
    {
        _hardwareService = new HardwareService();
        _reliabilityService = new ReliabilityService();
        _eventLogService = new EventLogService();
        
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SelectedDaysBack))
            {
                _ = LoadDataAsync();
            }
        };
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        StatusMessage = "正在加载数据...";

        await Task.Run(() =>
        {
            // Hardware
            StatusMessage = "正在获取硬件信息...";
            HardwareInfo = _hardwareService.GetHardwareInfo();

            // Reliability
            StatusMessage = "正在获取可靠性记录...";
            var reliability = _reliabilityService.GetReliabilityRecords(7);
            App.Current.Dispatcher.Invoke(() =>
            {
                ReliabilityRecords.Clear();
                foreach (var record in reliability)
                    ReliabilityRecords.Add(record);
            });

            // Events
            StatusMessage = $"正在获取事件日志 (最近{SelectedDaysBack}天)...";
            var events = _eventLogService.GetErrorEvents(SelectedDaysBack);
            App.Current.Dispatcher.Invoke(() =>
            {
                EventLogEntries.Clear();
                foreach (var evt in events)
                    EventLogEntries.Add(evt);
            });
        });

        StatusMessage = $"数据加载完成 | 可靠性记录: {ReliabilityRecords.Count} | 错误事件: {EventLogEntries.Count}";
        IsLoading = false;
    }

    [RelayCommand]
    private async Task ExportToHtmlAsync()
    {
        try
        {
            StatusMessage = "正在导出HTML报告...";
            IsLoading = true;

            await Task.Run(() =>
            {
                var html = GenerateHtmlReport();
                var fileName = $"DigYourWindows_Report_{DateTime.Now:yyyyMMdd_HHmmss}.html";
                var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
                
                File.WriteAllText(filePath, html, Encoding.UTF8);
                
                App.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = $"报告已导出: {fileName}";
                    Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                });
            });
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
