//! Real WMI (Windows Management Instrumentation) implementation
//! 
//! This module provides actual WMI access to Windows system information

use anyhow::{anyhow, Result};
use serde::{Deserialize, Serialize};
use std::process::Command;
use std::sync::mpsc;
use std::time::Duration;

// Data structures for WMI queries (re-exported from wmi.rs)
pub use crate::wmi::{UsbDevice, UsbController, ReliabilityRecord, LogEvent};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct SystemInfo {
    pub computer_name: Option<String>,
    pub os_name: Option<String>,
    pub os_version: Option<String>,
    pub os_build: Option<String>,
    pub system_type: Option<String>,
    pub total_memory: Option<u64>,
    pub free_memory: Option<u64>,
    pub cpu_count: Option<u32>,
    pub cpu_name: Option<String>,
    pub cpu_speed: Option<u64>,
    pub bios_version: Option<String>,
    pub bios_serial: Option<String>,
    pub motherboard: Option<String>,
    pub gpu_name: Option<String>,
    pub disk_drives: Vec<DiskDrive>,
    pub network_adapters: Vec<NetworkAdapter>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct DiskDrive {
    pub model: Option<String>,
    pub size: Option<u64>,
    pub interface_type: Option<String>,
    pub serial_number: Option<String>,
    pub health_status: Option<String>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct NetworkAdapter {
    pub name: Option<String>,
    pub description: Option<String>,
    pub mac_address: Option<String>,
    pub ip_address: Option<String>,
    pub default_gateway: Option<String>,
    pub speed: Option<u64>,
    pub status: Option<String>,
}

const WMIC_TIMEOUT_SECS: u64 = 30;

/// 在单独的线程中执行 WMIC 命令，并在超时时间后返回错误，避免主程序被卡住
fn run_wmic_with_timeout(args: &[&str]) -> Result<std::process::Output> {
    let args_vec: Vec<String> = args.iter().map(|s| s.to_string()).collect();
    let (tx, rx) = mpsc::channel();

    std::thread::spawn(move || {
        let output = Command::new("wmic").args(&args_vec).output();
        let _ = tx.send(output);
    });

    match rx.recv_timeout(Duration::from_secs(WMIC_TIMEOUT_SECS)) {
        Ok(Ok(output)) => {
            if output.status.success() {
                Ok(output)
            } else {
                let stderr = String::from_utf8_lossy(&output.stderr);
                if stderr.to_lowercase().contains("access is denied") {
                    eprintln!("[WARN] WMIC 访问被拒绝，部分数据可能不完整，建议以管理员权限运行 DigYourWindows。");
                }
                Err(anyhow!("WMIC command failed: {}", stderr.trim()))
            }
        }
        Ok(Err(e)) => Err(anyhow!("Failed to execute WMIC command: {}", e)),
        Err(_) => {
            Err(anyhow!(
                "WMIC command timed out after {} seconds, skipped to keep application responsive",
                WMIC_TIMEOUT_SECS
            ))
        }
    }
}

/// Get USB devices using WMI via command line
/// 
/// This implementation uses WMIC.exe which is available on Windows systems
pub fn get_usb_devices() -> Result<Vec<UsbDevice>> {
    let output = run_wmic_with_timeout(&[
        "path", "win32_usbdevice", "get",
        "DeviceID,Name,Description,Manufacturer,PNPDeviceID,Status",
        "/format:csv",
    ])?;

    let csv_output = String::from_utf8_lossy(&output.stdout);
    parse_csv_to_usb_devices(&csv_output)
}

/// Get USB controllers using WMI via command line
pub fn get_usb_controllers() -> Result<Vec<UsbController>> {
    let output = run_wmic_with_timeout(&[
        "path", "win32_usbcontroller", "get",
        "DeviceID,Name,Description,Manufacturer,Caption",
        "/format:csv",
    ])?;

    let csv_output = String::from_utf8_lossy(&output.stdout);
    parse_csv_to_usb_controllers(&csv_output)
}

/// Get reliability records using WMI via command line
pub fn get_reliability_records() -> Result<Vec<ReliabilityRecord>> {
    let output = run_wmic_with_timeout(&[
        "path",
        "win32_reliabilityrecords",
        "get",
        "TimeGenerated,ProductName,Message,RecordType,ComputerName",
        "/format:csv",
    ])?;

    let csv_output = String::from_utf8_lossy(&output.stdout);
    parse_csv_to_reliability_records(&csv_output)
}

/// Get error events from Event Log using WMI via command line
pub fn get_error_events(days: i64) -> Result<Vec<LogEvent>> {
    let time_filter = format!("TimeGenerated>='{}'", 
        (chrono::Local::now() - chrono::Duration::days(days))
            .format("%Y%m%d%H%M%S%.f%z")
    );
    
    let where_filter = format!("({time_filter} and (Type='Error' or Type='Warning'))");
    let output = run_wmic_with_timeout(&[
        "path",
        "win32_ntlogevent",
        "where",
        &where_filter,
        "get",
        "TimeGenerated,SourceName,Message,Type,Logfile,EventCode,ComputerName",
        "/format:csv",
    ])?;

    let csv_output = String::from_utf8_lossy(&output.stdout);
    parse_csv_to_log_events(&csv_output)
}

// Parse CSV output from WMIC to structured data
fn parse_csv_to_usb_devices(csv: &str) -> Result<Vec<UsbDevice>> {
    let mut devices = Vec::new();
    
    // Skip header line and process each record
    for line in csv.lines().skip(1) {
        if line.trim().is_empty() {
            continue;
        }
        
        let fields: Vec<&str> = line.split(',').collect();
        if fields.len() < 6 {
            continue;
        }
        
        // WMIC CSV format: Node,DeviceID,Name,Description,Manufacturer,PNPDeviceID,Status
        let device = UsbDevice {
            device_id: fields.get(1).unwrap_or(&"").trim_matches('"').to_string(),
            name: Some(fields.get(2).unwrap_or(&"").trim_matches('"').to_string()),
            description: Some(fields.get(3).unwrap_or(&"").trim_matches('"').to_string()),
            manufacturer: Some(fields.get(4).unwrap_or(&"").trim_matches('"').to_string()),
            pnp_device_id: Some(fields.get(5).unwrap_or(&"").trim_matches('"').to_string()),
            status: Some(fields.get(6).unwrap_or(&"").trim_matches('"').to_string()),
        };
        
        devices.push(device);
    }
    
    Ok(devices)
}

fn parse_csv_to_usb_controllers(csv: &str) -> Result<Vec<UsbController>> {
    let mut controllers = Vec::new();
    
    // Skip header line and process each record
    for line in csv.lines().skip(1) {
        if line.trim().is_empty() {
            continue;
        }
        
        let fields: Vec<&str> = line.split(',').collect();
        if fields.len() < 5 {
            continue;
        }
        
        // WMIC CSV format: Node,DeviceID,Name,Description,Manufacturer,Caption
        let controller = UsbController {
            device_id: fields.get(1).unwrap_or(&"").trim_matches('"').to_string(),
            name: Some(fields.get(2).unwrap_or(&"").trim_matches('"').to_string()),
            description: Some(fields.get(3).unwrap_or(&"").trim_matches('"').to_string()),
            manufacturer: Some(fields.get(4).unwrap_or(&"").trim_matches('"').to_string()),
            caption: Some(fields.get(5).unwrap_or(&"").trim_matches('"').to_string()),
            protocol_version: determine_usb_version(fields.get(2).unwrap_or(&"").trim_matches('"')),
        };
        
        controllers.push(controller);
    }
    
    Ok(controllers)
}

fn parse_csv_to_reliability_records(csv: &str) -> Result<Vec<ReliabilityRecord>> {
    let mut records = Vec::new();
    
    // Skip header line and process each record
    for line in csv.lines().skip(1) {
        if line.trim().is_empty() {
            continue;
        }
        
        let fields: Vec<&str> = line.split(',').collect();
        if fields.len() < 5 {
            continue;
        }
        
        // WMIC CSV format: Node,TimeGenerated,ProductName,Message,RecordType,ComputerName
        let record = ReliabilityRecord {
            time_generated: fields.get(1).unwrap_or(&"").trim_matches('"').to_string(),
            product_name: Some(fields.get(2).unwrap_or(&"").trim_matches('"').to_string()),
            message: Some(fields.get(3).unwrap_or(&"").trim_matches('"').to_string()),
            record_type: fields.get(4).unwrap_or(&"").trim_matches('"').parse().ok(),
            computer_name: Some(fields.get(5).unwrap_or(&"").trim_matches('"').to_string()),
        };
        
        records.push(record);
    }
    
    Ok(records)
}

fn parse_csv_to_log_events(csv: &str) -> Result<Vec<LogEvent>> {
    let mut events = Vec::new();
    
    // Skip header line and process each record
    for line in csv.lines().skip(1) {
        if line.trim().is_empty() {
            continue;
        }
        
        let fields: Vec<&str> = line.split(',').collect();
        if fields.len() < 7 {
            continue;
        }
        
        // WMIC CSV format: Node,TimeGenerated,SourceName,Message,Type,Logfile,EventCode,ComputerName
        let event = LogEvent {
            time_generated: fields.get(1).unwrap_or(&"").trim_matches('"').to_string(),
            source_name: Some(fields.get(2).unwrap_or(&"").trim_matches('"').to_string()),
            message: Some(fields.get(3).unwrap_or(&"").trim_matches('"').to_string()),
            event_type: Some(fields.get(4).unwrap_or(&"").trim_matches('"').to_string()),
            log_file: Some(fields.get(5).unwrap_or(&"").trim_matches('"').to_string()),
        };
        
        events.push(event);
    }
    
    Ok(events)
}

/// Analyze and categorize events for better reporting
pub fn analyze_events(events: &[LogEvent]) -> EventAnalysis {
    let mut error_count = 0;
    let mut warning_count = 0;
    let mut source_counts = std::collections::HashMap::new();
    let mut log_file_counts = std::collections::HashMap::new();
    let mut critical_events = Vec::new();
    
    for event in events {
        match event.event_type.as_ref().map(|s| s.as_str()).unwrap_or("") {
            "Error" => error_count += 1,
            "Warning" => warning_count += 1,
            _ => {}
        }
        
        if let Some(source) = &event.source_name {
            *source_counts.entry(source.clone()).or_insert(0) += 1;
        }
        
        if let Some(log_file) = &event.log_file {
            *log_file_counts.entry(log_file.clone()).or_insert(0) += 1;
        }
        
        // Identify critical events (custom logic)
        if is_critical_event(event) {
            critical_events.push(event.to_owned());
        }
    }
    
    // Sort sources by frequency
    let mut top_sources: Vec<(String, u32)> = source_counts.into_iter().collect();
    top_sources.sort_by(|a, b| b.1.cmp(&a.1));
    
    // Sort log files by frequency
    let mut top_log_files: Vec<(String, u32)> = log_file_counts.into_iter().collect();
    top_log_files.sort_by(|a, b| b.1.cmp(&a.1));
    
    EventAnalysis {
        total_events: events.len(),
        error_count,
        warning_count,
        top_sources: top_sources.into_iter().take(10).collect(),
        top_log_files: top_log_files.into_iter().take(5).collect(),
        critical_events,
    }
}

/// Determine if an event is critical based on its properties
fn is_critical_event(event: &LogEvent) -> bool {
    if event.event_type.as_ref().map(|s| s.as_str()).unwrap_or("") != "Error" {
        return false;
    }
    
    // Check for critical sources
    if let Some(source) = &event.source_name {
        let critical_sources = [
            "BugCheck",
            "Microsoft-Windows-WER-SystemErrorReporting",
            "Microsoft-Windows-Eventlog",
            "Kernel-Power",
            "Disk",
            "NTFS",
        ];
        
        if critical_sources.iter().any(|&cs| source.contains(cs)) {
            return true;
        }
    }
    
    // Check for critical messages
    if let Some(message) = &event.message {
        let critical_keywords = [
            "stop",
            "crash",
            "blue screen",
            "fatal",
            "exception",
            "dump",
            "corrupt",
            "failure",
            "timeout",
            "unreachable",
        ];
        
        let lower_message = message.to_lowercase();
        if critical_keywords.iter().any(|&kw| lower_message.contains(kw)) {
            return true;
        }
    }
    
    false
}

/// Event analysis results
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct EventAnalysis {
    pub total_events: usize,
    pub error_count: u32,
    pub warning_count: u32,
    pub top_sources: Vec<(String, u32)>,
    pub top_log_files: Vec<(String, u32)>,
    pub critical_events: Vec<LogEvent>,
}

// Helper function to determine USB version from controller name
fn determine_usb_version(controller_name: &str) -> Option<String> {
    if controller_name.to_lowercase().contains("usb 3") || controller_name.to_lowercase().contains("xhci") {
        Some("USB 3.0".to_string())
    } else if controller_name.to_lowercase().contains("usb 2") || controller_name.to_lowercase().contains("ehci") {
        Some("USB 2.0".to_string())
    } else if controller_name.to_lowercase().contains("usb 1") || controller_name.to_lowercase().contains("uhci") {
        Some("USB 1.1".to_string())
    } else {
        None
    }
}

/// Get comprehensive system information using WMI
pub fn get_system_info() -> Result<SystemInfo> {
    let system_info = SystemInfo {
        computer_name: get_computer_name()?,
        os_name: get_os_name()?,
        os_version: get_os_version()?,
        os_build: get_os_build()?,
        system_type: get_system_type()?,
        total_memory: get_total_memory()?,
        free_memory: get_free_memory()?,
        cpu_count: get_cpu_count()?,
        cpu_name: get_cpu_name()?,
        cpu_speed: get_cpu_speed()?,
        bios_version: get_bios_version()?,
        bios_serial: get_bios_serial()?,
        motherboard: get_motherboard()?,
        gpu_name: get_gpu_name()?,
        disk_drives: get_disk_drives()?,
        network_adapters: get_network_adapters()?,
    };
    
    Ok(system_info)
}

// Helper functions to get specific system information
fn get_computer_name() -> Result<Option<String>> {
    let output = Command::new("wmic")
        .args(&["computersystem", "get", "name", "/format:csv"])
        .output()?;
    
    if output.status.success() {
        let csv = String::from_utf8_lossy(&output.stdout);
        for line in csv.lines().skip(1) {
            if let Some(name) = line.split(',').nth(1) {
                return Ok(Some(name.trim_matches('"').to_string()));
            }
        }
    }
    
    Ok(None)
}

fn get_os_name() -> Result<Option<String>> {
    let output = Command::new("wmic")
        .args(&["os", "get", "caption", "/format:csv"])
        .output()?;
    
    if output.status.success() {
        let csv = String::from_utf8_lossy(&output.stdout);
        for line in csv.lines().skip(1) {
            if let Some(caption) = line.split(',').nth(1) {
                return Ok(Some(caption.trim_matches('"').to_string()));
            }
        }
    }
    
    Ok(None)
}

fn get_os_version() -> Result<Option<String>> {
    let output = Command::new("wmic")
        .args(&["os", "get", "version", "/format:csv"])
        .output()?;
    
    if output.status.success() {
        let csv = String::from_utf8_lossy(&output.stdout);
        for line in csv.lines().skip(1) {
            if let Some(version) = line.split(',').nth(1) {
                return Ok(Some(version.trim_matches('"').to_string()));
            }
        }
    }
    
    Ok(None)
}

fn get_os_build() -> Result<Option<String>> {
    let output = Command::new("wmic")
        .args(&["os", "get", "buildnumber", "/format:csv"])
        .output()?;
    
    if output.status.success() {
        let csv = String::from_utf8_lossy(&output.stdout);
        for line in csv.lines().skip(1) {
            if let Some(build) = line.split(',').nth(1) {
                return Ok(Some(build.trim_matches('"').to_string()));
            }
        }
    }
    
    Ok(None)
}

fn get_system_type() -> Result<Option<String>> {
    let output = Command::new("wmic")
        .args(&["computersystem", "get", "systemtype", "/format:csv"])
        .output()?;
    
    if output.status.success() {
        let csv = String::from_utf8_lossy(&output.stdout);
        for line in csv.lines().skip(1) {
            if let Some(sys_type) = line.split(',').nth(1) {
                return Ok(Some(sys_type.trim_matches('"').to_string()));
            }
        }
    }
    
    Ok(None)
}

fn get_total_memory() -> Result<Option<u64>> {
    let output = Command::new("wmic")
        .args(&["computersystem", "get", "totalphysicalmemory", "/format:csv"])
        .output()?;
    
    if output.status.success() {
        let csv = String::from_utf8_lossy(&output.stdout);
        for line in csv.lines().skip(1) {
            if let Some(mem_str) = line.split(',').nth(1) {
                let mem_str = mem_str.trim_matches('"');
                if let Ok(mem) = mem_str.parse::<u64>() {
                    return Ok(Some(mem));
                }
            }
        }
    }
    
    Ok(None)
}

fn get_free_memory() -> Result<Option<u64>> {
    let output = Command::new("wmic")
        .args(&["os", "get", "freephysicalmemory", "/format:csv"])
        .output()?;
    
    if output.status.success() {
        let csv = String::from_utf8_lossy(&output.stdout);
        for line in csv.lines().skip(1) {
            if let Some(mem_str) = line.split(',').nth(1) {
                let mem_str = mem_str.trim_matches('"');
                if let Ok(mem_kb) = mem_str.parse::<u64>() {
                    // Convert KB to bytes
                    return Ok(Some(mem_kb * 1024));
                }
            }
        }
    }
    
    Ok(None)
}

fn get_cpu_count() -> Result<Option<u32>> {
    let output = Command::new("wmic")
        .args(&["computersystem", "get", "numberofprocessors", "/format:csv"])
        .output()?;
    
    if output.status.success() {
        let csv = String::from_utf8_lossy(&output.stdout);
        for line in csv.lines().skip(1) {
            if let Some(count_str) = line.split(',').nth(1) {
                let count_str = count_str.trim_matches('"');
                if let Ok(count) = count_str.parse::<u32>() {
                    return Ok(Some(count));
                }
            }
        }
    }
    
    Ok(None)
}

fn get_cpu_name() -> Result<Option<String>> {
    let output = Command::new("wmic")
        .args(&["cpu", "get", "name", "/format:csv"])
        .output()?;
    
    if output.status.success() {
        let csv = String::from_utf8_lossy(&output.stdout);
        for line in csv.lines().skip(1) {
            if let Some(name) = line.split(',').nth(1) {
                return Ok(Some(name.trim_matches('"').to_string()));
            }
        }
    }
    
    Ok(None)
}

fn get_cpu_speed() -> Result<Option<u64>> {
    let output = Command::new("wmic")
        .args(&["cpu", "get", "maxclockspeed", "/format:csv"])
        .output()?;
    
    if output.status.success() {
        let csv = String::from_utf8_lossy(&output.stdout);
        for line in csv.lines().skip(1) {
            if let Some(speed_str) = line.split(',').nth(1) {
                let speed_str = speed_str.trim_matches('"');
                if let Ok(speed) = speed_str.parse::<u64>() {
                    // Convert MHz to Hz
                    return Ok(Some(speed * 1000000));
                }
            }
        }
    }
    
    Ok(None)
}

fn get_bios_version() -> Result<Option<String>> {
    let output = Command::new("wmic")
        .args(&["bios", "get", "version", "/format:csv"])
        .output()?;
    
    if output.status.success() {
        let csv = String::from_utf8_lossy(&output.stdout);
        for line in csv.lines().skip(1) {
            if let Some(version) = line.split(',').nth(1) {
                return Ok(Some(version.trim_matches('"').to_string()));
            }
        }
    }
    
    Ok(None)
}

fn get_bios_serial() -> Result<Option<String>> {
    let output = Command::new("wmic")
        .args(&["bios", "get", "serialnumber", "/format:csv"])
        .output()?;
    
    if output.status.success() {
        let csv = String::from_utf8_lossy(&output.stdout);
        for line in csv.lines().skip(1) {
            if let Some(serial) = line.split(',').nth(1) {
                return Ok(Some(serial.trim_matches('"').to_string()));
            }
        }
    }
    
    Ok(None)
}

fn get_motherboard() -> Result<Option<String>> {
    let output = Command::new("wmic")
        .args(&["baseboard", "get", "product", "/format:csv"])
        .output()?;
    
    if output.status.success() {
        let csv = String::from_utf8_lossy(&output.stdout);
        for line in csv.lines().skip(1) {
            if let Some(product) = line.split(',').nth(1) {
                return Ok(Some(product.trim_matches('"').to_string()));
            }
        }
    }
    
    Ok(None)
}

fn get_gpu_name() -> Result<Option<String>> {
    let output = Command::new("wmic")
        .args(&["path", "win32_videocontroller", "get", "name", "/format:csv"])
        .output()?;
    
    if output.status.success() {
        let csv = String::from_utf8_lossy(&output.stdout);
        for line in csv.lines().skip(1) {
            if let Some(name) = line.split(',').nth(1) {
                return Ok(Some(name.trim_matches('"').to_string()));
            }
        }
    }
    
    Ok(None)
}

fn get_disk_drives() -> Result<Vec<DiskDrive>> {
    let output = Command::new("wmic")
        .args(&["diskdrive", "get", "model,size,interfacetype,serialnumber", "/format:csv"])
        .output()?;
    
    let mut drives = Vec::new();
    
    if output.status.success() {
        let csv = String::from_utf8_lossy(&output.stdout);
        for line in csv.lines().skip(1) {
            if line.trim().is_empty() {
                continue;
            }
            
            let fields: Vec<&str> = line.split(',').collect();
            if fields.len() < 5 {
                continue;
            }
            
            let size = fields.get(2).unwrap_or(&"").trim_matches('"');
            let size = if !size.is_empty() {
                size.parse::<u64>().ok()
            } else {
                None
            };
            
            let drive = DiskDrive {
                model: Some(fields.get(1).unwrap_or(&"").trim_matches('"').to_string()),
                size,
                interface_type: Some(fields.get(3).unwrap_or(&"").trim_matches('"').to_string()),
                serial_number: Some(fields.get(4).unwrap_or(&"").trim_matches('"').to_string()),
                health_status: None, // Would need additional WMI query
            };
            
            drives.push(drive);
        }
    }
    
    Ok(drives)
}

fn get_network_adapters() -> Result<Vec<NetworkAdapter>> {
    let output = Command::new("wmic")
        .args(&["nicconfig", "get", "description,macaddress,ipaddress,defaultipgateway,speed", "/format:csv"])
        .output()?;
    
    let mut adapters = Vec::new();
    
    if output.status.success() {
        let csv = String::from_utf8_lossy(&output.stdout);
        for line in csv.lines().skip(1) {
            if line.trim().is_empty() {
                continue;
            }
            
            let fields: Vec<&str> = line.split(',').collect();
            if fields.len() < 6 {
                continue;
            }
            
            let ip_address = {
                let ip_str = fields.get(3).unwrap_or(&"").trim_matches('"');
                if !ip_str.is_empty() && ip_str != "{" {
                    Some(ip_str.trim_start_matches('{').trim_end_matches('}').to_string())
                } else {
                    None
                }
            };
            
            let default_gateway = {
                let gw_str = fields.get(4).unwrap_or(&"").trim_matches('"');
                if !gw_str.is_empty() && gw_str != "{" {
                    Some(gw_str.trim_start_matches('{').trim_end_matches('}').to_string())
                } else {
                    None
                }
            };
            
            let speed = fields.get(5).unwrap_or(&"").trim_matches('"');
            let speed = if !speed.is_empty() {
                speed.parse::<u64>().ok()
            } else {
                None
            };
            
            let adapter = NetworkAdapter {
                name: None, // Would need additional WMI query
                description: Some(fields.get(1).unwrap_or(&"").trim_matches('"').to_string()),
                mac_address: Some(fields.get(2).unwrap_or(&"").trim_matches('"').to_string()),
                ip_address,
                default_gateway,
                speed,
                status: None, // Would need additional WMI query
            };
            
            adapters.push(adapter);
        }
    }
    
    Ok(adapters)
}