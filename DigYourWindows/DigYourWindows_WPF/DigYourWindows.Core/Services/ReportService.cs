using System.Text;
using System.Text.Json;
using DigYourWindows.Core.Models;

namespace DigYourWindows.Core.Services;

public class ReportService
{
    public string SerializeToJson(DiagnosticData data, bool indented = true)
    {
        return JsonSerializer.Serialize(
            data,
            new JsonSerializerOptions
            {
                WriteIndented = indented
            });
    }

    public DiagnosticData? DeserializeFromJson(string json)
    {
        return JsonSerializer.Deserialize<DiagnosticData>(json);
    }

    public string GenerateHtmlReport(DiagnosticData data, int daysBackForEvents, int maxEvents = 100)
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
        sb.AppendLine($"    <h1 class='mb-4'>Windows 诊断报告 - {data.CollectedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}</h1>");

        sb.AppendLine("    <div class='card'>");
        sb.AppendLine("        <div class='card-header'><h3>系统概览</h3></div>");
        sb.AppendLine("        <div class='card-body'>");
        sb.AppendLine("            <div class='row'>");
        sb.AppendLine($"                <div class='col-md-3'><strong>计算机名:</strong> {data.Hardware.ComputerName}</div>");
        sb.AppendLine($"                <div class='col-md-3'><strong>操作系统:</strong> {data.Hardware.OsVersion}</div>");
        sb.AppendLine($"                <div class='col-md-3'><strong>CPU:</strong> {data.Hardware.CpuName}</div>");
        sb.AppendLine($"                <div class='col-md-3'><strong>内存:</strong> {data.Hardware.TotalMemoryMB} MB</div>");
        sb.AppendLine("            </div>");
        sb.AppendLine("        </div>");
        sb.AppendLine("    </div>");

        if (data.Performance != null)
        {
            sb.AppendLine("    <div class='card'>");
            sb.AppendLine("        <div class='card-header'><h3>系统性能分析</h3></div>");
            sb.AppendLine("        <div class='card-body'>");
            sb.AppendLine("            <div class='row mb-3'>");
            sb.AppendLine("                <div class='col-md-3'>");
            sb.AppendLine("                    <div class='card text-center p-3'>");
            sb.AppendLine("                        <h5>系统健康评分</h5>");
            sb.AppendLine($"                        <div class='metric' style='color: {data.Performance.HealthColor}'>{data.Performance.SystemHealthScore:F0}/100</div>");
            sb.AppendLine($"                        <span class='badge bg-secondary'>{data.Performance.HealthGrade}</span>");
            sb.AppendLine("                    </div>");
            sb.AppendLine("                </div>");
            sb.AppendLine("                <div class='col-md-3'>");
            sb.AppendLine("                    <div class='card text-center p-3'>");
            sb.AppendLine("                        <h5>稳定性评分</h5>");
            sb.AppendLine($"                        <div class='metric'>{data.Performance.StabilityScore:F0}/100</div>");
            sb.AppendLine("                    </div>");
            sb.AppendLine("                </div>");
            sb.AppendLine("                <div class='col-md-3'>");
            sb.AppendLine("                    <div class='card text-center p-3'>");
            sb.AppendLine("                        <h5>性能评分</h5>");
            sb.AppendLine($"                        <div class='metric'>{data.Performance.PerformanceScore:F0}/100</div>");
            sb.AppendLine("                    </div>");
            sb.AppendLine("                </div>");
            sb.AppendLine("                <div class='col-md-3'>");
            sb.AppendLine("                    <div class='card text-center p-3'>");
            sb.AppendLine("                        <h5>内存评分</h5>");
            sb.AppendLine($"                        <div class='metric'>{data.Performance.MemoryUsageScore:F0}/100</div>");
            sb.AppendLine("                    </div>");
            sb.AppendLine("                </div>");
            sb.AppendLine("            </div>");
            sb.AppendLine("            <div class='row'>");
            sb.AppendLine("                <div class='col-md-3'>");
            sb.AppendLine("                    <div class='card text-center p-3'>");
            sb.AppendLine("                        <h5>磁盘健康</h5>");
            sb.AppendLine($"                        <div class='metric'>{data.Performance.DiskHealthScore:F0}/100</div>");
            sb.AppendLine("                    </div>");
            sb.AppendLine("                </div>");
            sb.AppendLine("                <div class='col-md-3'>");
            sb.AppendLine("                    <div class='card text-center p-3'>");
            sb.AppendLine("                        <h5>关键问题</h5>");
            sb.AppendLine($"                        <div class='metric text-danger'>{data.Performance.CriticalIssuesCount}</div>");
            sb.AppendLine("                    </div>");
            sb.AppendLine("                </div>");
            sb.AppendLine("                <div class='col-md-3'>");
            sb.AppendLine("                    <div class='card text-center p-3'>");
            sb.AppendLine("                        <h5>警告数量</h5>");
            sb.AppendLine($"                        <div class='metric text-warning'>{data.Performance.WarningsCount}</div>");
            sb.AppendLine("                    </div>");
            sb.AppendLine("                </div>");
            sb.AppendLine("                <div class='col-md-3'>");
            sb.AppendLine("                    <div class='card text-center p-3'>");
            sb.AppendLine("                        <h5>系统运行时间</h5>");
            sb.AppendLine($"                        <div class='metric'>{data.Performance.SystemUptimeDays:F0} 天</div>");
            sb.AppendLine("                    </div>");
            sb.AppendLine("                </div>");
            sb.AppendLine("            </div>");

            if (data.Performance.Recommendations.Any())
            {
                sb.AppendLine("            <div class='mt-4'>");
                sb.AppendLine("                <h5>优化建议</h5>");
                sb.AppendLine("                <ul>");
                foreach (var recommendation in data.Performance.Recommendations)
                {
                    sb.AppendLine($"                    <li>{recommendation}</li>");
                }
                sb.AppendLine("                </ul>");
                sb.AppendLine("            </div>");
            }

            sb.AppendLine("        </div>");
            sb.AppendLine("    </div>");
        }

        if (data.Hardware.Gpus.Count > 0)
        {
            sb.AppendLine("    <div class='card'>");
            sb.AppendLine("        <div class='card-header'><h3>GPU 信息</h3></div>");
            sb.AppendLine("        <div class='card-body'>");
            sb.AppendLine("            <table class='table'>");
            sb.AppendLine("                <thead><tr><th>名称</th><th>温度</th><th>负载</th><th>显存</th><th>核心频率</th><th>功耗</th></tr></thead>");
            sb.AppendLine("                <tbody>");
            foreach (var gpu in data.Hardware.Gpus)
            {
                sb.AppendLine($"                    <tr><td>{gpu.Name}</td><td>{gpu.Temperature:F1}°C</td><td>{gpu.Load:F1}%</td><td>{gpu.MemoryUsed:F0}/{gpu.MemoryTotal:F0} MB</td><td>{gpu.CoreClock:F0} MHz</td><td>{gpu.Power:F1} W</td></tr>");
            }
            sb.AppendLine("                </tbody>");
            sb.AppendLine("            </table>");
            sb.AppendLine("        </div>");
            sb.AppendLine("    </div>");
        }

        sb.AppendLine("    <div class='card'>");
        sb.AppendLine($"        <div class='card-header'><h3>错误日志 (最近{daysBackForEvents}天) - {data.Events.Count} 条</h3></div>");
        sb.AppendLine("        <div class='card-body'>");
        sb.AppendLine("            <table class='table table-sm table-striped'>");
        sb.AppendLine("                <thead><tr><th>时间</th><th>来源</th><th>类型</th><th>ID</th><th>消息</th></tr></thead>");
        sb.AppendLine("                <tbody>");
        foreach (var evt in data.Events.Take(maxEvents))
        {
            var msg = evt.Message;
            if (msg.Length > 100)
            {
                msg = msg.Substring(0, 100);
            }

            sb.AppendLine($"                    <tr><td>{evt.TimeGenerated:yyyy-MM-dd HH:mm}</td><td>{evt.SourceName}</td><td>{evt.EventType}</td><td>{evt.EventId}</td><td>{msg}</td></tr>");
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
