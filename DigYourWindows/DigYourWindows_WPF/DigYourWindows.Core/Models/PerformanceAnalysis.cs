namespace DigYourWindows.Core.Models;

public record PerformanceAnalysis
{
    /// <summary>
    /// 系统健康评分 (0-100，分数越高越好)
    /// </summary>
    public float SystemHealthScore { get; init; }

    /// <summary>
    /// 系统稳定性评分 (0-100，分数越高越好)
    /// </summary>
    public float StabilityScore { get; init; }

    /// <summary>
    /// 性能评分 (0-100，分数越高越好)
    /// </summary>
    public float PerformanceScore { get; init; }

    /// <summary>
    /// 内存使用评分 (0-100，分数越高越好)
    /// </summary>
    public float MemoryUsageScore { get; init; }

    /// <summary>
    /// 磁盘健康评分 (0-100，分数越高越好)
    /// </summary>
    public float DiskHealthScore { get; init; }

    /// <summary>
    /// 系统运行天数
    /// </summary>
    public float? SystemUptimeDays { get; init; }

    /// <summary>
    /// 关键问题数量
    /// </summary>
    public int CriticalIssuesCount { get; init; }

    /// <summary>
    /// 警告数量
    /// </summary>
    public int WarningsCount { get; init; }

    /// <summary>
    /// 性能指标详情
    /// </summary>
    public Dictionary<string, float> PerformanceMetrics { get; init; } = new();

    /// <summary>
    /// 优化建议列表
    /// </summary>
    public List<string> Recommendations { get; init; } = new();

    /// <summary>
    /// 获取健康评分等级描述
    /// </summary>
    public string HealthGrade => SystemHealthScore switch
    {
        >= 90 => "优秀",
        >= 75 => "良好",
        >= 60 => "一般",
        >= 40 => "较差",
        _ => "需要优化"
    };

    /// <summary>
    /// 获取健康评分对应的颜色
    /// </summary>
    public string HealthColor => SystemHealthScore switch
    {
        >= 90 => "#28a745", // 绿色
        >= 75 => "#17a2b8", // 青色
        >= 60 => "#ffc107", // 黄色
        >= 40 => "#fd7e14", // 橙色
        _ => "#dc3545"      // 红色
    };
}