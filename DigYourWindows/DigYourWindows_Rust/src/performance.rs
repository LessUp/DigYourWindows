//! Performance analysis module for Windows system performance metrics

use std::collections::HashMap;
use serde::{Serialize, Deserialize};
use crate::hardware::HardwareData;
use crate::events::{LogEvent, get_event_analysis};
use crate::reliability::ReliabilityRecord;

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct PerformanceAnalysis {
    pub system_health_score: f32,  // 0-100, higher is better
    pub stability_score: f32,      // 0-100, higher is better
    pub performance_score: f32,     // 0-100, higher is better
    pub memory_usage_score: f32,   // 0-100, higher is better
    pub disk_health_score: f32,    // 0-100, higher is better
    pub system_uptime_days: Option<f32>,
    pub critical_issues_count: u32,
    pub warnings_count: u32,
    pub performance_metrics: HashMap<String, f32>,
    pub recommendations: Vec<String>,
}

/// Analyze system performance and health
pub fn analyze_system_performance(
    hardware: &HardwareData,
    events: &[LogEvent],
    reliability: &[ReliabilityRecord]
) -> PerformanceAnalysis {
    eprintln!("[INFO] Performing system performance analysis");
    
    // Analyze events to get detailed statistics
    let event_analysis = get_event_analysis(events);
    
    // Initialize metrics
    let mut performance_metrics = HashMap::new();
    let mut recommendations = Vec::new();
    
    // Calculate memory usage score (based on total memory)
    let total_memory_gb = hardware.total_memory as f32 / 1024.0 / 1024.0 / 1024.0;
    let memory_usage_score = calculate_memory_score(total_memory_gb, &mut recommendations);
    performance_metrics.insert("memory_score".to_string(), memory_usage_score);
    
    // Calculate disk health score
    let disk_health_score = calculate_disk_score(&hardware.disks, &mut recommendations);
    performance_metrics.insert("disk_health".to_string(), disk_health_score);
    
    // Calculate system uptime (placeholder - would need WMI query)
    let system_uptime_days = Some(7.0); // Default to a week as placeholder
    
    // Calculate stability score based on critical errors and reliability records
    let stability_score = calculate_stability_score(
        event_analysis.error_count,
        event_analysis.warning_count,
        event_analysis.critical_events.len(),
        reliability.len(),
        &mut recommendations
    );
    performance_metrics.insert("stability".to_string(), stability_score);
    
    // Calculate performance score based on CPU and other factors
    let performance_score = calculate_performance_score(
        hardware.cpu_count,
        &hardware.cpu_brand,
        total_memory_gb,
        &mut recommendations
    );
    performance_metrics.insert("performance".to_string(), performance_score);
    
    // Calculate overall system health score
    let system_health_score = (stability_score * 0.4 + performance_score * 0.3 + 
                               memory_usage_score * 0.15 + disk_health_score * 0.15).max(0.0).min(100.0);
    
    // Add summary metric
    performance_metrics.insert("overall_health".to_string(), system_health_score);
    
    // Generate additional recommendations
    if event_analysis.critical_events.len() > 0 {
        recommendations.push(format!(
            "发现 {} 个严重系统错误，建议立即检查系统日志", 
            event_analysis.critical_events.len()
        ));
    }
    
    if system_health_score < 60.0 {
        recommendations.push("系统健康评分较低，建议进行全面系统维护".to_string());
    }
    
    PerformanceAnalysis {
        system_health_score,
        stability_score,
        performance_score,
        memory_usage_score,
        disk_health_score,
        system_uptime_days,
        critical_issues_count: event_analysis.critical_events.len() as u32,
        warnings_count: event_analysis.warning_count,
        performance_metrics,
        recommendations,
    }
}

/// Calculate memory score based on total memory
fn calculate_memory_score(total_memory_gb: f32, recommendations: &mut Vec<String>) -> f32 {
    let mut score: f32 = 50.0; // Base score
    
    // Adjust score based on memory size
    if total_memory_gb >= 16.0 {
        score = 90.0; // Excellent
    } else if total_memory_gb >= 8.0 {
        score = 75.0; // Good
    } else if total_memory_gb >= 4.0 {
        score = 60.0; // Acceptable
        recommendations.push("内存容量较小，建议考虑升级到8GB或更多以提升性能".to_string());
    } else {
        score = 40.0; // Poor
        recommendations.push("内存容量严重不足，强烈建议升级到8GB或更多".to_string());
    }
    
    score
}

/// Calculate disk health score
fn calculate_disk_score(disks: &[crate::hardware::DiskInfo], recommendations: &mut Vec<String>) -> f32 {
    if disks.is_empty() {
        recommendations.push("未检测到磁盘信息，请检查磁盘连接".to_string());
        return 50.0;
    }
    
    let mut total_score: f32 = 0.0;
    let mut disk_count = 0;
    
    for disk in disks {
        disk_count += 1;
        
        // Calculate free space percentage
        let free_percentage = (disk.available_space as f32 / disk.total_space as f32) * 100.0;
        
        let mut disk_score: f32 = 50.0; // Base score
        
        if free_percentage > 50.0 {
            disk_score = 90.0; // Excellent
        } else if free_percentage > 25.0 {
            disk_score = 75.0; // Good
        } else if free_percentage > 10.0 {
            disk_score = 60.0; // Acceptable
            recommendations.push(format!(
                "磁盘 {} 剩余空间不足 ({}%)，建议清理空间", 
                disk.name, 
                free_percentage.round()
            ));
        } else {
            disk_score = 30.0; // Poor
            recommendations.push(format!(
                "磁盘 {} 剩余空间严重不足 ({}%)，请立即清理空间", 
                disk.name, 
                free_percentage.round()
            ));
        }
        
        total_score += disk_score;
    }
    
    total_score / disk_count as f32
}

/// Calculate stability score
fn calculate_stability_score(
    error_count: u32,
    warning_count: u32,
    critical_events_count: usize,
    reliability_records_count: usize,
    recommendations: &mut Vec<String>
) -> f32 {
    let mut score: f32 = 100.0; // Start with perfect score
    
    // Deduct points for errors
    score -= (error_count as f32 * 2.0).min(40.0);
    
    // Deduct points for warnings
    score -= (warning_count as f32 * 0.5).min(20.0);
    
    // Deduct points for critical events
    score -= (critical_events_count as f32 * 10.0).min(30.0);
    
    // Check for reliability issues
    if reliability_records_count > 50 {
        score -= 10.0;
        recommendations.push("系统可靠性记录较多，建议检查系统稳定性".to_string());
    }
    
    score.max(0.0)
}

/// Calculate performance score
fn calculate_performance_score(
    cpu_count: usize,
    cpu_brand: &str,
    total_memory_gb: f32,
    recommendations: &mut Vec<String>
) -> f32 {
    let mut score: f32 = 50.0; // Base score
    
    // CPU performance evaluation
    if cpu_count >= 8 {
        score += 20.0; // Excellent
    } else if cpu_count >= 4 {
        score += 15.0; // Good
    } else if cpu_count >= 2 {
        score += 5.0; // Acceptable
    } else {
        score -= 10.0; // Poor
        recommendations.push("CPU核心数较少，可能会影响多任务处理性能".to_string());
    }
    
    // CPU brand evaluation
    if cpu_brand.to_lowercase().contains("intel") {
        if cpu_brand.to_lowercase().contains("i9") || cpu_brand.to_lowercase().contains("xeon") {
            score += 15.0; // High-end
        } else if cpu_brand.to_lowercase().contains("i7") {
            score += 10.0; // Mid-high
        } else if cpu_brand.to_lowercase().contains("i5") {
            score += 5.0; // Mid-range
        }
    } else if cpu_brand.to_lowercase().contains("amd") {
        if cpu_brand.to_lowercase().contains("ryzen 9") || cpu_brand.to_lowercase().contains("threadripper") {
            score += 15.0; // High-end
        } else if cpu_brand.to_lowercase().contains("ryzen 7") {
            score += 10.0; // Mid-high
        } else if cpu_brand.to_lowercase().contains("ryzen 5") {
            score += 5.0; // Mid-range
        }
    }
    
    // Memory evaluation
    if total_memory_gb >= 16.0 {
        score += 15.0; // Excellent
    } else if total_memory_gb >= 8.0 {
        score += 10.0; // Good
    } else if total_memory_gb >= 4.0 {
        score += 5.0; // Acceptable
    } else {
        score -= 5.0; // Poor
    }
    
    score.max(0.0).min(100.0)
}