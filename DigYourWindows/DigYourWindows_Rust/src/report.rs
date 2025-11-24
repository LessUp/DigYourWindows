use tera::{Tera, Context};
use std::fs::File;
use std::io::Write;
use crate::hardware::HardwareData;
use crate::reliability::ReliabilityRecord;
use crate::events::LogEvent;

pub fn generate_report(
    hardware: &HardwareData,
    reliability: &[ReliabilityRecord],
    events: &[LogEvent]
) -> Result<(), Box<dyn std::error::Error>> {
    let mut tera = Tera::default();
    tera.add_raw_template("report", include_str!("template.html"))?;

    let mut context = Context::new();
    context.insert("timestamp", &chrono::Local::now().format("%Y-%m-%d %H:%M:%S").to_string());
    context.insert("hardware", hardware);
    context.insert("reliability", reliability);
    context.insert("events", events);

    let rendered = tera.render("report", &context)?;

    let mut file = File::create("report.html")?;
    file.write_all(rendered.as_bytes())?;
    
    println!("Report generated: report.html");

    Ok(())
}
