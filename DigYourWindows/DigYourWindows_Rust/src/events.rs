use chrono::{Duration, Local};
use serde::{Deserialize, Serialize};
use wmi::{COMLibrary, WMIConnection};

#[derive(Deserialize, Serialize, Debug)]
pub struct LogEvent {
    #[serde(rename = "TimeGenerated")]
    pub time_generated: String, 
    #[serde(rename = "SourceName")]
    pub source_name: Option<String>,
    #[serde(rename = "Message")]
    pub message: Option<String>,
    #[serde(rename = "Type")]
    pub event_type: Option<String>, 
    #[serde(rename = "Logfile")]
    pub log_file: Option<String>,
}

pub fn get_error_events(days: i64) -> Vec<LogEvent> {
    let com_con = match COMLibrary::new() {
        Ok(c) => c,
        Err(_) => return vec![],
    };
    let wmi_con = match WMIConnection::new(com_con) {
        Ok(c) => c,
        Err(_) => return vec![],
    };

    let start_time = Local::now() - Duration::days(days);
    // Format: YYYYMMDDHHMMSS.uuuuuu+OOO
    // Using +000 for simplicity, might miss timezone nuances but good enough for "recent" filter
    let time_str = start_time.format("%Y%m%d%H%M%S.000000+000").to_string();
    
    // Query for Error or Warning in System/Application logs
    let query = format!(
        "SELECT TimeGenerated, SourceName, Message, Type, Logfile FROM Win32_NTLogEvent WHERE (Type='Error' OR Type='Warning') AND (Logfile='System' OR Logfile='Application') AND TimeGenerated >= '{}'",
        time_str
    );

    match wmi_con.raw_query(&query) {
        Ok(results) => results,
        Err(e) => {
            eprintln!("Failed to query Win32_NTLogEvent: {:?}", e);
            vec![]
        }
    }
}
