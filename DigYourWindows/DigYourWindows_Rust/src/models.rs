//! Standardized data models for DigYourWindows
//! 
//! This module defines the canonical data structures used across both
//! Rust CLI and WPF GUI versions, ensuring cross-version compatibility.

use chrono::{DateTime, Utc};
use serde::{Deserialize, Serialize};

/// Complete diagnostic data collected from the system
#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct DiagnosticData {
    pub hardware: HardwareData,
    pub reliability: Vec<ReliabilityRecord>,
    pub events: Vec<LogEvent>,
    pub performance: PerformanceAnalysis,
    pub collected_at: DateTime<Utc>,
}

/// Hardware information collected from the system
#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct HardwareData {
    pub computer_name: String,
    pub os_version: String,
    pub cpu_brand: String,
    pub cpu_cores: u32,
    pub total_memory: u64,
    pub disks: Vec<DiskInfo>,
    pub network_adapters: Vec<NetworkAdapter>,
    pub usb_devices: Vec<UsbDevice>,
    pub usb_controllers: Vec<UsbController>,
    pub gpus: Vec<GpuInfo>,
}

/// Disk information
#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct DiskInfo {
    pub name: String,
    pub file_system: String,
    pub total_space: u64,
    pub available_space: u64,
}

/// Network adapter information
#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct NetworkAdapter {
    pub name: String,
    pub mac_address: String,
    pub ip_addresses: Vec<String>,
}

/// USB device information
#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct UsbDevice {
    pub device_id: String,
    pub name: Option<String>,
    pub description: Option<String>,
    pub manufacturer: Option<String>,
    pub pnp_device_id: Option<String>,
    pub status: Option<String>,
}

/// USB controller information
#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct UsbController {
    pub device_id: String,
    pub name: Option<String>,
    pub description: Option<String>,
    pub manufacturer: Option<String>,
    pub caption: Option<String>,
    pub protocol_version: Option<String>,
}

/// GPU information
#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct GpuInfo {
    pub name: String,
    pub driver_version: Option<String>,
    pub video_memory: Option<u64>,
}

/// Windows reliability record
#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct ReliabilityRecord {
    pub timestamp: DateTime<Utc>,
    pub source_name: String,
    pub message: String,
    pub event_type: String,
}

/// Windows event log entry
#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct LogEvent {
    pub time_generated: DateTime<Utc>,
    pub log_file: String,
    pub source_name: String,
    pub event_type: String,
    pub event_id: u32,
    pub message: String,
}

/// Performance analysis results
#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct PerformanceAnalysis {
    pub system_health_score: f64,
    pub stability_score: f64,
    pub performance_score: f64,
    pub memory_usage_score: f64,
    pub disk_health_score: f64,
    pub critical_issues_count: u32,
    pub warnings_count: u32,
    pub recommendations: Vec<String>,
    pub health_grade: String,
    pub health_color: String,
}

impl DiagnosticData {
    /// Create a new DiagnosticData instance with current timestamp
    pub fn new(
        hardware: HardwareData,
        reliability: Vec<ReliabilityRecord>,
        events: Vec<LogEvent>,
        performance: PerformanceAnalysis,
    ) -> Self {
        Self {
            hardware,
            reliability,
            events,
            performance,
            collected_at: Utc::now(),
        }
    }
}

impl HardwareData {
    /// Create a new HardwareData instance
    pub fn new(
        computer_name: String,
        os_version: String,
        cpu_brand: String,
        cpu_cores: u32,
        total_memory: u64,
    ) -> Self {
        Self {
            computer_name,
            os_version,
            cpu_brand,
            cpu_cores,
            total_memory,
            disks: Vec::new(),
            network_adapters: Vec::new(),
            usb_devices: Vec::new(),
            usb_controllers: Vec::new(),
            gpus: Vec::new(),
        }
    }
}

impl PerformanceAnalysis {
    /// Create a new PerformanceAnalysis with default values
    pub fn new() -> Self {
        Self {
            system_health_score: 0.0,
            stability_score: 0.0,
            performance_score: 0.0,
            memory_usage_score: 0.0,
            disk_health_score: 0.0,
            critical_issues_count: 0,
            warnings_count: 0,
            recommendations: Vec::new(),
            health_grade: String::from("Unknown"),
            health_color: String::from("#808080"),
        }
    }
    
    /// Calculate health grade based on system health score
    pub fn calculate_health_grade(&mut self) {
        let (grade, color) = match self.system_health_score {
            s if s >= 90.0 => ("Excellent", "#4CAF50"),
            s if s >= 75.0 => ("Good", "#8BC34A"),
            s if s >= 60.0 => ("Fair", "#FFC107"),
            s if s >= 40.0 => ("Poor", "#FF9800"),
            _ => ("Critical", "#F44336"),
        };
        self.health_grade = grade.to_string();
        self.health_color = color.to_string();
    }
}

impl Default for PerformanceAnalysis {
    fn default() -> Self {
        Self::new()
    }
}
