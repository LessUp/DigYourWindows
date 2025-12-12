//! Property-based tests for JSON round-trip serialization
//! 
//! **Feature: digyourwindows-improvements, Property 7: JSON Serialization Round-Trip**
//! **Validates: Requirements 2.5**

use proptest::prelude::*;
use DigYourWindows_Rust::models::*;

/// Generator for valid DiagnosticData
fn arb_diagnostic_data() -> impl Strategy<Value = DiagnosticData> {
    (
        arb_hardware_data(),
        prop::collection::vec(arb_reliability_record(), 0..10),
        prop::collection::vec(arb_log_event(), 0..20),
        arb_performance_analysis(),
    ).prop_map(|(hardware, reliability, events, performance)| {
        DiagnosticData::new(hardware, reliability, events, performance)
    })
}

/// Generator for HardwareData
fn arb_hardware_data() -> impl Strategy<Value = HardwareData> {
    (
        "[A-Z]{2,10}",  // computer_name
        "Windows (10|11).*",  // os_version
        "(Intel|AMD).*",  // cpu_brand
        1u32..128,  // cpu_cores
        1024u64..1024*1024*1024*128,  // total_memory (up to 128GB)
        prop::collection::vec(arb_disk_info(), 0..5),
        prop::collection::vec(arb_network_adapter(), 0..5),
        prop::collection::vec(arb_usb_device(), 0..10),
        prop::collection::vec(arb_usb_controller(), 0..5),
        prop::collection::vec(arb_gpu_info(), 0..3),
    ).prop_map(|(computer_name, os_version, cpu_brand, cpu_cores, total_memory, 
                 disks, network_adapters, usb_devices, usb_controllers, gpus)| {
        HardwareData {
            computer_name,
            os_version,
            cpu_brand,
            cpu_cores,
            total_memory,
            disks,
            network_adapters,
            usb_devices,
            usb_controllers,
            gpus,
        }
    })
}

/// Generator for DiskInfo
fn arb_disk_info() -> impl Strategy<Value = DiskInfo> {
    (
        "[A-Z]:\\\\",  // name
        "(NTFS|FAT32|exFAT)",  // file_system
        1024u64..1024*1024*1024*1024*4,  // total_space (up to 4TB)
    ).prop_flat_map(|(name, file_system, total_space)| {
        (Just(name), Just(file_system), Just(total_space), 0u64..=total_space)
    }).prop_map(|(name, file_system, total_space, available_space)| {
        DiskInfo {
            name,
            file_system,
            total_space,
            available_space,
        }
    })
}

/// Generator for NetworkAdapter
fn arb_network_adapter() -> impl Strategy<Value = NetworkAdapter> {
    (
        "[A-Za-z0-9 ]+",  // name
        "[0-9A-F]{2}:[0-9A-F]{2}:[0-9A-F]{2}:[0-9A-F]{2}:[0-9A-F]{2}:[0-9A-F]{2}",  // mac_address
        prop::collection::vec("(192\\.168|10\\.0|172\\.16)\\.[0-9]{1,3}\\.[0-9]{1,3}", 0..3),  // ip_addresses
    ).prop_map(|(name, mac_address, ip_addresses)| {
        NetworkAdapter {
            name,
            mac_address,
            ip_addresses,
        }
    })
}

/// Generator for UsbDevice
fn arb_usb_device() -> impl Strategy<Value = UsbDevice> {
    (
        "USB\\\\VID_[0-9A-F]{4}&PID_[0-9A-F]{4}.*",  // device_id
        prop::option::of("[A-Za-z0-9 ]+"),  // name
        prop::option::of("[A-Za-z0-9 ]+"),  // description
        prop::option::of("[A-Za-z0-9 ]+"),  // manufacturer
        prop::option::of("USB\\\\VID_[0-9A-F]{4}&PID_[0-9A-F]{4}.*"),  // pnp_device_id
        prop::option::of("(OK|Error|Unknown)"),  // status
    ).prop_map(|(device_id, name, description, manufacturer, pnp_device_id, status)| {
        UsbDevice {
            device_id,
            name,
            description,
            manufacturer,
            pnp_device_id,
            status,
        }
    })
}

/// Generator for UsbController
fn arb_usb_controller() -> impl Strategy<Value = UsbController> {
    (
        "USB\\\\ROOT_HUB[0-9]{2}.*",  // device_id
        prop::option::of("USB Root Hub.*"),  // name
        prop::option::of("USB [0-9]\\.[0-9] Root Hub"),  // description
        prop::option::of("[A-Za-z0-9 ]+"),  // manufacturer
        prop::option::of("USB Root Hub.*"),  // caption
        prop::option::of("USB (2\\.0|3\\.0|3\\.1|4\\.0)"),  // protocol_version
    ).prop_map(|(device_id, name, description, manufacturer, caption, protocol_version)| {
        UsbController {
            device_id,
            name,
            description,
            manufacturer,
            caption,
            protocol_version,
        }
    })
}

/// Generator for GpuInfo
fn arb_gpu_info() -> impl Strategy<Value = GpuInfo> {
    (
        "(NVIDIA|AMD|Intel).*",  // name
        prop::option::of("[0-9]+\\.[0-9]+\\.[0-9]+"),  // driver_version
        prop::option::of(1024u64..1024*1024*1024*32),  // video_memory (up to 32GB)
    ).prop_map(|(name, driver_version, video_memory)| {
        GpuInfo {
            name,
            driver_version,
            video_memory,
        }
    })
}

/// Generator for ReliabilityRecord
fn arb_reliability_record() -> impl Strategy<Value = ReliabilityRecord> {
    (
        "[A-Za-z0-9 ]+",  // source_name
        ".{10,200}",  // message
        "(Application Failure|Windows Failure|Warning|Information)",  // event_type
    ).prop_map(|(source_name, message, event_type)| {
        ReliabilityRecord {
            timestamp: chrono::Utc::now(),
            source_name,
            message,
            event_type,
        }
    })
}

/// Generator for LogEvent
fn arb_log_event() -> impl Strategy<Value = LogEvent> {
    (
        "(System|Application|Security)",  // log_file
        "[A-Za-z0-9-]+",  // source_name
        "(Error|Warning|Information)",  // event_type
        0u32..65535,  // event_id
        ".{10,200}",  // message
    ).prop_map(|(log_file, source_name, event_type, event_id, message)| {
        LogEvent {
            time_generated: chrono::Utc::now(),
            log_file,
            source_name,
            event_type,
            event_id,
            message,
        }
    })
}

/// Generator for PerformanceAnalysis
fn arb_performance_analysis() -> impl Strategy<Value = PerformanceAnalysis> {
    (
        0.0f64..=100.0,  // system_health_score
        0.0f64..=100.0,  // stability_score
        0.0f64..=100.0,  // performance_score
        0.0f64..=100.0,  // memory_usage_score
        0.0f64..=100.0,  // disk_health_score
        0u32..100,  // critical_issues_count
        0u32..200,  // warnings_count
        prop::collection::vec("[A-Za-z0-9 ]+", 0..10),  // recommendations
    ).prop_map(|(system_health_score, stability_score, performance_score, 
                 memory_usage_score, disk_health_score, critical_issues_count, 
                 warnings_count, recommendations)| {
        let health_grade = match system_health_score {
            s if s >= 90.0 => "Excellent",
            s if s >= 75.0 => "Good",
            s if s >= 60.0 => "Fair",
            s if s >= 40.0 => "Poor",
            _ => "Critical",
        }.to_string();
        
        let health_color = match system_health_score {
            s if s >= 90.0 => "#4CAF50",
            s if s >= 75.0 => "#8BC34A",
            s if s >= 60.0 => "#FFC107",
            s if s >= 40.0 => "#FF9800",
            _ => "#F44336",
        }.to_string();
        
        PerformanceAnalysis {
            system_health_score,
            stability_score,
            performance_score,
            memory_usage_score,
            disk_health_score,
            critical_issues_count,
            warnings_count,
            recommendations,
            health_grade,
            health_color,
        }
    })
}

proptest! {
    #![proptest_config(ProptestConfig::with_cases(100))]
    
    /// Property 7: JSON Serialization Round-Trip
    /// For any DiagnosticData instance, serializing to JSON and then deserializing
    /// should produce a data structure equivalent to the original (allowing for
    /// floating-point precision differences).
    #[test]
    fn prop_json_roundtrip_preserves_data(data in arb_diagnostic_data()) {
        // Serialize to JSON
        let json_string = serde_json::to_string(&data)
            .expect("Failed to serialize DiagnosticData to JSON");
        
        // Deserialize back
        let deserialized: DiagnosticData = serde_json::from_str(&json_string)
            .expect("Failed to deserialize JSON back to DiagnosticData");
        
        // Verify hardware data
        prop_assert_eq!(deserialized.hardware.computer_name, data.hardware.computer_name);
        prop_assert_eq!(deserialized.hardware.os_version, data.hardware.os_version);
        prop_assert_eq!(deserialized.hardware.cpu_brand, data.hardware.cpu_brand);
        prop_assert_eq!(deserialized.hardware.cpu_cores, data.hardware.cpu_cores);
        prop_assert_eq!(deserialized.hardware.total_memory, data.hardware.total_memory);
        prop_assert_eq!(deserialized.hardware.disks.len(), data.hardware.disks.len());
        prop_assert_eq!(deserialized.hardware.network_adapters.len(), data.hardware.network_adapters.len());
        prop_assert_eq!(deserialized.hardware.usb_devices.len(), data.hardware.usb_devices.len());
        prop_assert_eq!(deserialized.hardware.usb_controllers.len(), data.hardware.usb_controllers.len());
        prop_assert_eq!(deserialized.hardware.gpus.len(), data.hardware.gpus.len());
        
        // Verify collections
        prop_assert_eq!(deserialized.reliability.len(), data.reliability.len());
        prop_assert_eq!(deserialized.events.len(), data.events.len());
        
        // Verify performance scores (with floating-point tolerance)
        let tolerance = 0.0001;
        prop_assert!((deserialized.performance.system_health_score - data.performance.system_health_score).abs() < tolerance);
        prop_assert!((deserialized.performance.stability_score - data.performance.stability_score).abs() < tolerance);
        prop_assert!((deserialized.performance.performance_score - data.performance.performance_score).abs() < tolerance);
        prop_assert!((deserialized.performance.memory_usage_score - data.performance.memory_usage_score).abs() < tolerance);
        prop_assert!((deserialized.performance.disk_health_score - data.performance.disk_health_score).abs() < tolerance);
        
        prop_assert_eq!(deserialized.performance.critical_issues_count, data.performance.critical_issues_count);
        prop_assert_eq!(deserialized.performance.warnings_count, data.performance.warnings_count);
        prop_assert_eq!(deserialized.performance.health_grade, data.performance.health_grade);
        prop_assert_eq!(deserialized.performance.health_color, data.performance.health_color);
        prop_assert_eq!(deserialized.performance.recommendations.len(), data.performance.recommendations.len());
    }
    
    /// Property 7 (continued): Pretty-printed JSON should also round-trip correctly
    #[test]
    fn prop_pretty_json_roundtrip_preserves_data(data in arb_diagnostic_data()) {
        // Serialize to pretty-printed JSON
        let json_string = serde_json::to_string_pretty(&data)
            .expect("Failed to serialize DiagnosticData to pretty JSON");
        
        // Deserialize back
        let deserialized: DiagnosticData = serde_json::from_str(&json_string)
            .expect("Failed to deserialize pretty JSON back to DiagnosticData");
        
        // Verify key fields
        prop_assert_eq!(deserialized.hardware.computer_name, data.hardware.computer_name);
        prop_assert_eq!(deserialized.hardware.cpu_cores, data.hardware.cpu_cores);
        prop_assert_eq!(deserialized.reliability.len(), data.reliability.len());
        prop_assert_eq!(deserialized.events.len(), data.events.len());
        
        // Verify performance scores with tolerance
        let tolerance = 0.0001;
        prop_assert!((deserialized.performance.system_health_score - data.performance.system_health_score).abs() < tolerance);
    }
}
