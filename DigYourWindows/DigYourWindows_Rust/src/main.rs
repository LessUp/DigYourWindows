mod hardware;
mod reliability;
mod events;
mod report;
mod simple_report;
mod wmi;
mod wmi_impl;
mod wmi_provider;
mod logging;
mod performance;
pub mod models;

use clap::{Parser, ValueEnum};
use std::path::PathBuf;

#[derive(Copy, Clone, PartialEq, Eq, PartialOrd, Ord, ValueEnum, Debug)]
#[clap(rename_all = "lower")]
enum OutputFormat {
    /// 只生成 HTML 报告
    Html,
    /// 只生成 JSON 报告
    Json,
    /// 同时生成 HTML 和 JSON 报告
    Both,
}

#[derive(Parser)]
#[command(name = "DigYourWindows")]
#[command(about = "A tool to dig deep into Windows system information")]
#[command(version = "0.1.0")]
struct Cli {
    /// Number of days to look back for event logs (e.g. 1 / 7 / 30)
    #[arg(short, long, default_value_t = 3)]
    days: i64,
    
    /// Output format: html / json / both
    #[arg(short, long, value_enum, default_value_t = OutputFormat::Html)]
    format: OutputFormat,
    
    /// Output file name (without extension)
    #[arg(short, long, default_value = "report")]
    output: PathBuf,
    
    /// Output directory (default: current directory)
    #[arg(long)]
    output_dir: Option<PathBuf>,
    
    /// Skip opening the report automatically
    #[arg(long)]
    no_open: bool,
}

fn main() {
    let cli = Cli::parse();
    
    // Initialize logging
    let _ = logging::init_logger("digyourwindows.log", logging::LogLevel::Info);
    eprintln!("[INFO] Starting DigYourWindows Diagnosis...");
    
    // Hardware
    println!("[1/4] Fetching Hardware Info...");
    eprintln!("[INFO] Fetching hardware information");
    let hw = hardware::get_hardware_info();
    eprintln!("[INFO] Hardware info collected: CPU={}, RAM={}GB", hw.cpu_brand, hw.total_memory / 1024 / 1024 / 1024);
    println!("      - CPU: {}", hw.cpu_brand);
    println!("      - RAM: {} GB", hw.total_memory / 1024 / 1024 / 1024);

    // Reliability
    println!("[2/4] Fetching Reliability Records...");
    eprintln!("[INFO] Fetching reliability records");
    let rel = reliability::get_reliability_records();
    eprintln!("[INFO] Found {} reliability records", rel.len());
    println!("      - Found {} records", rel.len());

    // Events
    println!("[3/4] Fetching Event Logs (Last {} Days)...", cli.days);
    eprintln!("[INFO] Fetching error events for the last {} days", cli.days);
    let logs = events::get_error_events(cli.days);
    eprintln!("[INFO] Found {} error/warning events", logs.len());
    println!("      - Found {} error/warning events", logs.len());

    // Determine output base path (support custom directory)
    let mut base_path = cli.output.clone();
    if let Some(dir) = cli.output_dir.as_ref() {
        if let Err(e) = std::fs::create_dir_all(dir) {
            eprintln!("[WARN] Failed to create output directory '{}': {:?}", dir.display(), e);
        } else {
            base_path = dir.join(base_path);
        }
    }
    
    let html_path = base_path.with_extension("html");
    let json_path = base_path.with_extension("json");
    
    // Report
    let format_str = match cli.format {
        OutputFormat::Html => "HTML",
        OutputFormat::Json => "JSON",
        OutputFormat::Both => "HTML + JSON",
    };
    println!("[4/4] Generating {} Report...", format_str);
    eprintln!("[INFO] Generating {} report at base path {}", format_str, base_path.display());
    
    let result = match cli.format {
        OutputFormat::Json => {
            report::generate_json_report(&hw, &rel, &logs, &json_path)
        },
        OutputFormat::Html => {
            report::generate_report(&hw, &rel, &logs, &html_path.to_string_lossy())
        },
        OutputFormat::Both => {
            // 先生成 HTML，再生成 JSON，只要有一个失败就返回错误
            match report::generate_report(&hw, &rel, &logs, &html_path.to_string_lossy()) {
                Ok(_) => report::generate_json_report(&hw, &rel, &logs, &json_path),
                Err(e) => Err(e),
            }
        }
    };
    
    match result {
        Ok(_) => {
            match cli.format {
                OutputFormat::Html => {
                    eprintln!("[INFO] HTML report successfully generated at '{}'", html_path.display());
                }
                OutputFormat::Json => {
                    eprintln!("[INFO] JSON report successfully generated at '{}'", json_path.display());
                }
                OutputFormat::Both => {
                    eprintln!("[INFO] HTML report successfully generated at '{}'", html_path.display());
                    eprintln!("[INFO] JSON report successfully generated at '{}'", json_path.display());
                }
            }
            
            // Open the HTML report automatically when available
            #[cfg(target_os = "windows")]
            if !cli.no_open && matches!(cli.format, OutputFormat::Html | OutputFormat::Both) {
                use std::process::Command;
                let _ = Command::new("cmd")
                    .args(&["/C", "start", &html_path.to_string_lossy()])
                    .spawn();
            }
        }
        Err(e) => {
            eprintln!("[ERROR] Error generating report: {:?}", e);
        }
    }
    
    println!("Done! Press Enter to exit.");
    let _ = std::io::stdin().read_line(&mut String::new());
}

fn generate_json_report(
    hw: &hardware::HardwareData,
    rel: &Vec<reliability::ReliabilityRecord>,
    logs: &Vec<events::LogEvent>,
    output_path: &PathBuf
) -> Result<(), Box<dyn std::error::Error>> {
    use std::collections::HashMap;
    use serde_json::Value;
    
    let mut report = HashMap::new();
    report.insert("hardware".to_string(), serde_json::to_value(hw)?);
    report.insert("reliability".to_string(), serde_json::to_value(rel)?);
    report.insert("events".to_string(), serde_json::to_value(logs)?);
    report.insert("generated_at".to_string(), Value::String(chrono::Utc::now().to_rfc3339()));
    
    let json_str = serde_json::to_string_pretty(&report)?;
    std::fs::write(output_path, json_str)?;
    
    eprintln!("[INFO] JSON report successfully generated at '{}'", output_path.display());
    Ok(())
}