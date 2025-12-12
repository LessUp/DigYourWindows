//! Property-based tests for JSON schema validation
//! 
//! **Feature: digyourwindows-improvements, Property 3: Schema Validation Correctness**
//! **Validates: Requirements 2.1**

use proptest::prelude::*;
use serde_json::{json, Value};

// Import the models module
use DigYourWindows_Rust::models::*;

/// Load the JSON schema from file
fn load_schema() -> Value {
    let schema_path = std::path::Path::new(env!("CARGO_MANIFEST_DIR"))
        .parent()
        .unwrap()
        .join("diagnostic-data-schema.json");
    
    let schema_content = std::fs::read_to_string(&schema_path)
        .expect("Failed to read schema file");
    
    serde_json::from_str(&schema_content)
        .expect("Failed to parse schema JSON")
}

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

/// Generator for malformed JSON that violates the schema
fn arb_malformed_diagnostic_data() -> impl Strategy<Value = Value> {
    prop::sample::select(vec![
        // Missing required fields
        json!({
            "hardware": {},
            "reliability": [],
            "events": []
        }),
        // Invalid types
        json!({
            "hardware": {
                "computerName": "TEST",
                "osVersion": "Windows 10",
                "cpuBrand": "Intel",
                "cpuCores": "not-a-number",  // Should be integer
                "totalMemory": 8589934592u64,
                "disks": [],
                "networkAdapters": [],
                "usbDevices": [],
                "usbControllers": [],
                "gpus": []
            },
            "reliability": [],
            "events": [],
            "performance": {
                "systemHealthScore": 150.0,  // Out of range
                "stabilityScore": 75.0,
                "performanceScore": 80.0,
                "memoryUsageScore": 85.0,
                "diskHealthScore": 90.0,
                "criticalIssuesCount": 0,
                "warningsCount": 0,
                "recommendations": [],
                "healthGrade": "Good",
                "healthColor": "#8BC34A"
            },
            "collectedAt": "2024-01-01T00:00:00Z"
        }),
        // Invalid date format
        json!({
            "hardware": {
                "computerName": "TEST",
                "osVersion": "Windows 10",
                "cpuBrand": "Intel",
                "cpuCores": 4,
                "totalMemory": 8589934592u64,
                "disks": [],
                "networkAdapters": [],
                "usbDevices": [],
                "usbControllers": [],
                "gpus": []
            },
            "reliability": [],
            "events": [],
            "performance": {
                "systemHealthScore": 75.0,
                "stabilityScore": 75.0,
                "performanceScore": 80.0,
                "memoryUsageScore": 85.0,
                "diskHealthScore": 90.0,
                "criticalIssuesCount": 0,
                "warningsCount": 0,
                "recommendations": [],
                "healthGrade": "Good",
                "healthColor": "#8BC34A"
            },
            "collectedAt": "invalid-date"
        }),
    ])
}

proptest! {
    #![proptest_config(ProptestConfig::with_cases(100))]
    
    /// Property 3: Schema Validation Correctness
    /// For any JSON data that conforms to the DiagnosticData schema, parsing should succeed
    /// and produce a valid data structure; for any JSON data that violates the schema,
    /// parsing should fail with a validation error.
    #[test]
    fn prop_valid_data_passes_schema_validation(data in arb_diagnostic_data()) {
        // Serialize to JSON
        let json_value = serde_json::to_value(&data)
            .expect("Failed to serialize DiagnosticData");
        
        // Load schema
        let schema_value = load_schema();
        let schema = jsonschema::JSONSchema::compile(&schema_value)
            .expect("Failed to compile schema");
        
        // Validate against schema
        let validation_result = schema.validate(&json_value);
        
        // Valid data should pass validation
        if let Err(errors) = validation_result {
            let error_messages: Vec<String> = errors
                .map(|e| format!("{}", e))
                .collect();
            prop_assert!(
                false,
                "Valid DiagnosticData failed schema validation: {}",
                error_messages.join(", ")
            );
        }
        
        // Should be able to deserialize back
        let deserialized: DiagnosticData = serde_json::from_value(json_value.clone())
            .expect("Failed to deserialize valid JSON");
        
        // Basic sanity checks
        prop_assert!(deserialized.hardware.cpu_cores > 0);
        prop_assert!(deserialized.hardware.total_memory > 0);
        prop_assert!(deserialized.performance.system_health_score >= 0.0);
        prop_assert!(deserialized.performance.system_health_score <= 100.0);
    }
    
    /// Property 3 (continued): Malformed data should fail validation
    #[test]
    fn prop_malformed_data_fails_schema_validation(malformed_json in arb_malformed_diagnostic_data()) {
        // Load schema
        let schema_value = load_schema();
        let schema = jsonschema::JSONSchema::compile(&schema_value)
            .expect("Failed to compile schema");
        
        // Validate against schema
        let validation_result = schema.validate(&malformed_json);
        
        // Malformed data should fail validation OR fail deserialization
        let deserialization_result: Result<DiagnosticData, _> = 
            serde_json::from_value(malformed_json.clone());
        
        prop_assert!(
            validation_result.is_err() || deserialization_result.is_err(),
            "Malformed data passed both schema validation and deserialization: {:?}",
            malformed_json
        );
    }
}
