use tera::{Tera, Context};
use std::fs::File;
use std::io::Write;
use crate::hardware::HardwareData;
use crate::reliability::ReliabilityRecord;
use crate::events::{LogEvent, get_event_analysis};
use crate::performance::analyze_system_performance;

pub fn generate_report(
    hardware: &HardwareData,
    reliability: &[ReliabilityRecord],
    events: &[LogEvent],
    output_path: &str
) -> Result<(), Box<dyn std::error::Error>> {
    eprintln!("[INFO] Generating HTML report using template engine");
    
    let mut tera = Tera::default();
    
    match tera.add_raw_template("report", include_str!("template.html")) {
        Ok(_) => eprintln!("[INFO] Template loaded successfully"),
        Err(e) => {
            eprintln!("[ERROR] Failed to load template: {:?}", e);
            return Err(Box::new(e));
        }
    }

    // Analyze events for better reporting
    let event_analysis = get_event_analysis(events);
    eprintln!("[INFO] Event analysis completed: {} total events ({} errors, {} warnings)", 
        event_analysis.total_events, event_analysis.error_count, event_analysis.warning_count);
    
    // Perform performance analysis
    let performance_analysis = analyze_system_performance(hardware, events, reliability);
    eprintln!("[INFO] Performance analysis completed: System health score: {:.1}", 
        performance_analysis.system_health_score);

    let mut context = Context::new();
    context.insert("timestamp", &chrono::Local::now().format("%Y-%m-%d %H:%M:%S").to_string());
    context.insert("hardware", hardware);
    context.insert("reliability", reliability);
    context.insert("events", events);
    context.insert("event_analysis", &event_analysis);
    context.insert("performance", &performance_analysis);

    match tera.render("report", &context) {
        Ok(rendered) => {
            eprintln!("[INFO] Template rendered successfully, writing to file");
            
            match File::create(output_path) {
                Ok(mut file) => {
                    match file.write_all(rendered.as_bytes()) {
                        Ok(_) => {
                            eprintln!("[INFO] Report written to file: {}", output_path);
                            println!("Report generated: {}", output_path);
                            Ok(())
                        },
                        Err(e) => {
                            eprintln!("[ERROR] Failed to write to file: {:?}", e);
                            Err(Box::new(e))
                        }
                    }
                },
                Err(e) => {
                    eprintln!("[ERROR] Failed to create output file: {:?}", e);
                    Err(Box::new(e))
                }
            }
        },
        Err(e) => {
            eprintln!("[ERROR] Failed to render template: {:?}", e);
            Err(Box::new(e))
        }
    }
}

/// Generate a JSON report with all collected data
pub fn generate_json_report(
    hardware: &HardwareData,
    reliability: &[ReliabilityRecord],
    events: &[LogEvent],
    output_path: &std::path::Path,
) -> Result<(), Box<dyn std::error::Error>> {
    eprintln!("[INFO] Generating JSON report");
    
    // Analyze events
    let event_analysis = get_event_analysis(events);
    
    // Perform performance analysis
    let performance_analysis = analyze_system_performance(hardware, events, reliability);
    
    // Create report structure
    let report = serde_json::json!({
        "timestamp": chrono::Local::now().format("%Y-%m-%d %H:%M:%S").to_string(),
        "hardware": hardware,
        "reliability": reliability,
        "events": events,
        "event_analysis": event_analysis,
        "performance": performance_analysis
    });
    
    // Write to file
    let json_string = serde_json::to_string_pretty(&report)?;
    match std::fs::write(output_path, json_string) {
        Ok(_) => {
            eprintln!("[INFO] JSON report written to file: {}", output_path.display());
            println!("JSON Report generated: {}", output_path.display());
            Ok(())
        },
        Err(e) => {
            eprintln!("[ERROR] Failed to write JSON report: {:?}", e);
            Err(Box::new(e))
        }
    }
}
