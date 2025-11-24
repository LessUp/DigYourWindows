using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DigYourWindows.Core.Models;
using DigYourWindows.Core.Services;
using System.Collections.ObjectModel;

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
    private ObservableCollection<EventLogEntry> _eventLogEntries = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "就绪";

    public MainViewModel()
    {
        _hardwareService = new HardwareService();
        _reliabilityService = new ReliabilityService();
        _eventLogService = new EventLogService();
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
            StatusMessage = "正在获取事件日志 (最近3天)...";
            var events = _eventLogService.GetErrorEvents(3);
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
    private void ExportToHtml()
    {
        StatusMessage = "导出功能开发中...";
        // TODO: 实现导出到HTML的功能
    }
}
