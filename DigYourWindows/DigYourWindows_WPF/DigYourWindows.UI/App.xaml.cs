using System.Configuration;
using System.Data;
using System.Windows;
using DigYourWindows.Core.Services;
using DigYourWindows.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DigYourWindows.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    public IServiceProvider Services => _serviceProvider ?? throw new InvalidOperationException("Service provider not initialized.");

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);

        _serviceProvider = services.BuildServiceProvider();

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainViewModel>();

        services.AddSingleton<ILogService, FileLogService>();
        services.AddSingleton<ReportService>();
        services.AddSingleton<DiagnosticCollectorService>();

        services.AddSingleton<CpuMonitorService>();
        services.AddSingleton<NetworkMonitorService>();
        services.AddSingleton<GpuMonitorService>();
        services.AddSingleton<DiskSmartService>();
        services.AddSingleton<HardwareService>();
        services.AddSingleton<ReliabilityService>();
        services.AddSingleton<EventLogService>();
        services.AddSingleton<PerformanceService>();
    }
}
