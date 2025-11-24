mod hardware;
mod reliability;
mod events;
mod report;

fn main() {
    println!("Starting DigYourWindows Diagnosis...");
    
    // Hardware
    println!("[1/4] Fetching Hardware Info...");
    let hw = hardware::get_hardware_info();
    println!("      - CPU: {}", hw.cpu_brand);
    println!("      - RAM: {} GB", hw.total_memory / 1024 / 1024 / 1024);
    
    // Reliability
    println!("[2/4] Fetching Reliability Records...");
    let rel = reliability::get_reliability_records();
    println!("      - Found {} records", rel.len());
    
    // Events (3 days)
    println!("[3/4] Fetching Event Logs (Last 3 Days)...");
    let logs = events::get_error_events(3);
    println!("      - Found {} error/warning events", logs.len());
    
    // Report
    println!("[4/4] Generating HTML Report...");
    if let Err(e) = report::generate_report(&hw, &rel, &logs) {
        eprintln!("Error generating report: {:?}", e);
    } else {
        println!("Report successfully generated at 'report.html'");
        // Open the file automatically
        #[cfg(target_os = "windows")]
        {
            use std::process::Command;
            let _ = Command::new("cmd").args(&["/C", "start", "report.html"]).spawn();
        }
    }
    
    println!("Done! Press Enter to exit.");
    let _ = std::io::stdin().read_line(&mut String::new());
}
