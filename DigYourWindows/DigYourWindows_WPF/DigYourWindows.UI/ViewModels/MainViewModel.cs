using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DigYourWindows.Core.Models;
using DigYourWindows.Core.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Threading;
using ScottPlot.WPF;
using Microsoft.Win32;
using Wpf.Ui.Appearance;

namespace DigYourWindows.UI.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly DiagnosticCollectorService _collectorService;
    private readonly ReportService _reportService;
    private readonly CpuMonitorService _cpuMonitorService;
    private readonly NetworkMonitorService _networkMonitorService;
    private readonly ILogService _log;
    private readonly DispatcherTimer _cpuMonitorTimer;
    private CancellationTokenSource? _loadCts;
    private DiagnosticData? _currentData;
    private bool _reloadRequested;

    private const int NetworkHistoryCapacity = 60;
    private long? _lastNetworkBytesReceived;
    private long? _lastNetworkBytesSent;
    private DateTimeOffset? _lastNetworkSampleTime;
    private readonly List<DateTime> _networkHistoryTimes = new();
    private readonly List<double> _networkHistoryDownload = new();
    private readonly List<double> _networkHistoryUpload = new();

    [ObservableProperty]
    private HardwareData? _hardwareInfo;

    [ObservableProperty]
    private CpuInfoData _cpuInfo = new();

    [ObservableProperty]
    private double _networkDownloadMBps;

    [ObservableProperty]
    private double _networkUploadMBps;

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

    public WpfPlot ReliabilityTrendPlot { get; } = new();

    public WpfPlot NetworkTrafficPlot { get; } = new();

    public MainViewModel(
        DiagnosticCollectorService collectorService,
        ReportService reportService,
        CpuMonitorService cpuMonitorService,
        NetworkMonitorService networkMonitorService,
        ILogService log)
    {
        _collectorService = collectorService;
        _reportService = reportService;
        _cpuMonitorService = cpuMonitorService;
        _networkMonitorService = networkMonitorService;
        _log = log;

        _cpuMonitorTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _cpuMonitorTimer.Tick += CpuMonitorTimer_Tick;
        _cpuMonitorTimer.Start();

        UpdateCpuInfo();
        UpdateNetworkTraffic();

        UpdateReliabilityTrendPlot();
        UpdateNetworkTrafficPlot();
    }

    private void CpuMonitorTimer_Tick(object? sender, EventArgs e)
    {
        UpdateCpuInfo();
        UpdateNetworkTraffic();
    }

    private void UpdateCpuInfo()
    {
        CpuInfo = _cpuMonitorService.GetCpuInfo();
    }

    private void UpdateNetworkTraffic()
    {
        try
        {
            var now = DateTimeOffset.Now;
            var (bytesReceived, bytesSent) = _networkMonitorService.GetTotalBytes();

            if (_lastNetworkSampleTime is null || _lastNetworkBytesReceived is null || _lastNetworkBytesSent is null)
            {
                _lastNetworkSampleTime = now;
                _lastNetworkBytesReceived = bytesReceived;
                _lastNetworkBytesSent = bytesSent;

                NetworkDownloadMBps = 0d;
                NetworkUploadMBps = 0d;

                AppendNetworkHistory(now.LocalDateTime, 0d, 0d);
                UpdateNetworkTrafficPlot();
                return;
            }

            var dtSeconds = (now - _lastNetworkSampleTime.Value).TotalSeconds;
            if (dtSeconds <= 0)
            {
                return;
            }

            var deltaReceived = bytesReceived - _lastNetworkBytesReceived.Value;
            var deltaSent = bytesSent - _lastNetworkBytesSent.Value;

            if (deltaReceived < 0) deltaReceived = 0;
            if (deltaSent < 0) deltaSent = 0;

            var downloadMBps = deltaReceived / dtSeconds / 1024d / 1024d;
            var uploadMBps = deltaSent / dtSeconds / 1024d / 1024d;

            NetworkDownloadMBps = downloadMBps;
            NetworkUploadMBps = uploadMBps;

            _lastNetworkSampleTime = now;
            _lastNetworkBytesReceived = bytesReceived;
            _lastNetworkBytesSent = bytesSent;

            AppendNetworkHistory(now.LocalDateTime, downloadMBps, uploadMBps);
            UpdateNetworkTrafficPlot();
        }
        catch
        {
        }
    }

    private void AppendNetworkHistory(DateTime time, double downloadMBps, double uploadMBps)
    {
        _networkHistoryTimes.Add(time);
        _networkHistoryDownload.Add(downloadMBps);
        _networkHistoryUpload.Add(uploadMBps);

        while (_networkHistoryTimes.Count > NetworkHistoryCapacity)
        {
            _networkHistoryTimes.RemoveAt(0);
            _networkHistoryDownload.RemoveAt(0);
            _networkHistoryUpload.RemoveAt(0);
        }
    }

    private void UpdateNetworkTrafficPlot()
    {
        var plot = NetworkTrafficPlot.Plot;
        plot.Clear();

        plot.Title("网络流量 (最近60秒)");
        plot.XLabel("时间");
        plot.YLabel("MB/s");

        ApplyPlotTheme(plot);

        if (_networkHistoryTimes.Count == 0)
        {
            NetworkTrafficPlot.Refresh();
            return;
        }

        var xs = _networkHistoryTimes.ToArray();
        var downYs = _networkHistoryDownload.ToArray();
        var upYs = _networkHistoryUpload.ToArray();

        var down = plot.Add.Scatter(xs, downYs);
        down.LegendText = "下载";
        down.LineWidth = 2;
        down.MarkerSize = 0;

        var up = plot.Add.Scatter(xs, upYs);
        up.LegendText = "上传";
        up.LineWidth = 2;
        up.MarkerSize = 0;

        plot.Axes.DateTimeTicksBottom();
        plot.Axes.AutoScale();
        plot.ShowLegend();

        ApplyPlotTheme(plot);
        NetworkTrafficPlot.Refresh();
    }

    public void Dispose()
    {
        _cpuMonitorTimer.Stop();
        _cpuMonitorTimer.Tick -= CpuMonitorTimer_Tick;
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

            UpdateReliabilityTrendPlot();

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

            UpdateReliabilityTrendPlot();

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
        UpdateReliabilityTrendPlot();
        UpdateNetworkTrafficPlot();
        StatusMessage = $"主题已切换为: {(CurrentTheme == ApplicationTheme.Dark ? "深色" : "浅色")}";
    }

    private void UpdateReliabilityTrendPlot()
    {
        var plot = ReliabilityTrendPlot.Plot;
        plot.Clear();

        plot.Title("可靠性趋势");
        plot.XLabel("日期");
        plot.YLabel("记录数");

        var records = ReliabilityRecords.ToList();
        if (records.Count == 0)
        {
            ApplyPlotTheme(plot);
            ReliabilityTrendPlot.Refresh();
            return;
        }

        var endDate = DateTime.Today;
        var startDate = endDate.AddDays(-(SelectedDaysBack - 1));
        if (SelectedDaysBack <= 0)
        {
            startDate = records.Min(x => x.Timestamp.Date);
        }

        var days = Enumerable
            .Range(0, (endDate - startDate).Days + 1)
            .Select(i => startDate.AddDays(i))
            .ToArray();

        var totalYs = days
            .Select(day => (double)records.Count(r => r.Timestamp.Date == day))
            .ToArray();

        var hasSeries = false;
        var total = plot.Add.Scatter(days, totalYs);
        total.LegendText = "总计";
        total.LineWidth = 3;
        total.MarkerSize = 5;
        hasSeries = true;

        var categories = new[]
        {
            new { Key = 1, Name = "应用程序故障" },
            new { Key = 2, Name = "Windows 故障" },
            new { Key = 3, Name = "其他故障" },
            new { Key = 0, Name = "未知" }
        };

        foreach (var cat in categories)
        {
            double[] ys = cat.Key == 0
                ? days
                    .Select(day => (double)records.Count(r =>
                        (!r.RecordType.HasValue || (r.RecordType is not 1 and not 2 and not 3)) &&
                        r.Timestamp.Date == day))
                    .ToArray()
                : days
                    .Select(day => (double)records.Count(r =>
                        r.RecordType == cat.Key &&
                        r.Timestamp.Date == day))
                    .ToArray();

            if (ys.All(y => y == 0))
            {
                continue;
            }

            var series = plot.Add.Scatter(days, ys);
            series.LegendText = cat.Name;
            series.LineWidth = 2;
            series.MarkerSize = 4;
            hasSeries = true;
        }

        plot.Axes.DateTimeTicksBottom();
        plot.Axes.AutoScale();

        if (hasSeries)
        {
            plot.ShowLegend();
        }

        ApplyPlotTheme(plot);
        ReliabilityTrendPlot.Refresh();
    }

    private void ApplyPlotTheme(ScottPlot.Plot plot)
    {
        if (CurrentTheme == ApplicationTheme.Dark)
        {
            plot.Add.Palette = new ScottPlot.Palettes.Penumbra();

            plot.FigureBackground.Color = ScottPlot.Color.FromHex("#181818");
            plot.DataBackground.Color = ScottPlot.Color.FromHex("#1f1f1f");

            plot.Axes.Color(ScottPlot.Color.FromHex("#d7d7d7"));
            plot.Grid.MajorLineColor = ScottPlot.Color.FromHex("#404040");

            plot.Legend.BackgroundColor = ScottPlot.Color.FromHex("#404040");
            plot.Legend.FontColor = ScottPlot.Color.FromHex("#d7d7d7");
            plot.Legend.OutlineColor = ScottPlot.Color.FromHex("#d7d7d7");
        }
        else
        {
            plot.Add.Palette = new ScottPlot.Palettes.Category10();

            plot.FigureBackground.Color = ScottPlot.Color.FromHex("#ffffff");
            plot.DataBackground.Color = ScottPlot.Color.FromHex("#ffffff");

            plot.Axes.Color(ScottPlot.Color.FromHex("#333333"));
            plot.Grid.MajorLineColor = ScottPlot.Color.FromHex("#e0e0e0");

            plot.Legend.BackgroundColor = ScottPlot.Color.FromHex("#ffffff");
            plot.Legend.FontColor = ScottPlot.Color.FromHex("#333333");
            plot.Legend.OutlineColor = ScottPlot.Color.FromHex("#333333");
        }
    }
}
