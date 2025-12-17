using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DigYourWindows.Core.Models;
using DigYourWindows.Core.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;
using Wpf.Ui.Appearance;

namespace DigYourWindows.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly DiagnosticCollectorService _collectorService;
    private readonly ReportService _reportService;
    private readonly ILogService _log;
    private CancellationTokenSource? _loadCts;
    private DiagnosticData? _currentData;
    private bool _reloadRequested;

    [ObservableProperty]
    private HardwareData? _hardwareInfo;

    [ObservableProperty]
    private ObservableCollection<ReliabilityRecordData> _reliabilityRecords = new();

    [ObservableProperty]
    private ObservableCollection<LogEventData> _eventLogEntries = new();

    [ObservableProperty]
    private PerformanceAnalysisData? _performanceAnalysis;

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
        DiagnosticCollectorService collectorService,
        ReportService reportService,
        ILogService log)
    {
        _collectorService = collectorService;
        _reportService = reportService;
        _log = log;
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

        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();

        try
        {
            var progress = new Progress<DiagnosticCollectionProgress>(p =>
            {
                StatusMessage = p.Message;
            });

            var daysBack = SelectedDaysBack;
            var result = await _collectorService.CollectAsync(daysBack, progress, _loadCts.Token);
            _currentData = result.Data;

            HardwareInfo = result.Data.Hardware;

            ReliabilityRecords.Clear();
            foreach (var record in result.Data.Reliability)
            {
                ReliabilityRecords.Add(record);
            }

            EventLogEntries.Clear();
            foreach (var evt in result.Data.Events)
            {
                EventLogEntries.Add(evt);
            }

            PerformanceAnalysis = result.Data.Performance;

            if (result.Warnings.Count > 0)
            {
                _log.Warn($"数据采集存在 {result.Warnings.Count} 条警告: {string.Join(" | ", result.Warnings)}");
            }

            var performanceScore = result.Data.Performance.SystemHealthScore;
            StatusMessage = $"数据加载完成 | 可靠性记录: {ReliabilityRecords.Count} | 错误事件: {EventLogEntries.Count} | 系统健康评分: {performanceScore:F0}/100" +
                            (result.Warnings.Count > 0 ? $" | 警告: {result.Warnings.Count}" : string.Empty);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "已取消";
        }
        catch (Exception ex)
        {
            _log.Error("加载失败", ex);
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
    private async Task ImportFromJsonAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*",
            DefaultExt = ".json"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            StatusMessage = "正在导入JSON报告...";
            IsLoading = true;

            var json = await Task.Run(() => File.ReadAllText(dialog.FileName, Encoding.UTF8));
            var data = _reportService.DeserializeFromJson(json);

            if (data == null)
            {
                StatusMessage = "导入失败: JSON 解析结果为空";
                return;
            }

            _currentData = data;

            HardwareInfo = data.Hardware;

            ReliabilityRecords.Clear();
            foreach (var record in data.Reliability)
            {
                ReliabilityRecords.Add(record);
            }

            EventLogEntries.Clear();
            foreach (var evt in data.Events)
            {
                EventLogEntries.Add(evt);
            }

            PerformanceAnalysis = data.Performance;

            StatusMessage = $"JSON已导入 | 采集时间(UTC): {data.CollectedAt:yyyy-MM-dd HH:mm:ss} | 可靠性记录: {ReliabilityRecords.Count} | 错误事件: {EventLogEntries.Count}";
        }
        catch (Exception ex)
        {
            _log.Error("导入失败", ex);
            StatusMessage = $"导入失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private DiagnosticData BuildDiagnosticDataForExport()
    {
        var data = new DiagnosticData
        {
            Hardware = HardwareInfo ?? new HardwareData(),
            Reliability = ReliabilityRecords.ToList(),
            Events = EventLogEntries.ToList(),
            Performance = PerformanceAnalysis ?? new PerformanceAnalysisData(),
            CollectedAt = _currentData?.CollectedAt ?? DateTime.UtcNow
        };

        _currentData = data;
        return data;
    }

    [RelayCommand]
    private async Task ExportToJsonAsync()
    {
        try
        {
            StatusMessage = "正在导出JSON报告...";
            IsLoading = true;

            var data = BuildDiagnosticDataForExport();
            var json = _reportService.SerializeToJson(data, indented: true);

            var fileName = $"DigYourWindows_Report_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

            await Task.Run(() => File.WriteAllText(filePath, json, Encoding.UTF8));

            StatusMessage = $"JSON已导出: {fileName}";
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _log.Error("导出JSON失败", ex);
            StatusMessage = $"导出失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExportToHtmlAsync()
    {
        try
        {
            StatusMessage = "正在导出HTML报告...";
            IsLoading = true;

            var data = BuildDiagnosticDataForExport();
            var html = _reportService.GenerateHtmlReport(data, SelectedDaysBack);
            var fileName = $"DigYourWindows_Report_{DateTime.Now:yyyyMMdd_HHmmss}.html";
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

            await Task.Run(() => File.WriteAllText(filePath, html, Encoding.UTF8));

            StatusMessage = $"报告已导出: {fileName}";
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _log.Error("导出HTML失败", ex);
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
}
