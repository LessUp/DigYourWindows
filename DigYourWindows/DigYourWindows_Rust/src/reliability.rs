use serde::{Deserialize, Serialize};
use wmi::{COMLibrary, WMIConnection};

#[derive(Deserialize, Serialize, Debug)]
pub struct ReliabilityRecord {
    #[serde(rename = "TimeGenerated")]
    pub time_generated: String,
    #[serde(rename = "ProductName")]
    pub product_name: Option<String>,
    #[serde(rename = "Message")]
    pub message: Option<String>,
    #[serde(rename = "RecordType")]
    pub record_type: Option<u32>, // 1=App fail, 2=Win fail, 3=Misc fail, etc.
    #[serde(rename = "ComputerName")]
    pub computer_name: Option<String>,
}

pub fn get_reliability_records() -> Vec<ReliabilityRecord> {
    let com_con = match COMLibrary::new() {
        Ok(c) => c,
        Err(_) => return vec![],
    };
    let wmi_con = match WMIConnection::new(com_con) {
        Ok(c) => c,
        Err(_) => return vec![],
    };

    // We select all and filter in memory for simplicity, 
    // as WMI date queries are error prone without exact formatting.
    // Win32_ReliabilityRecords is typically in root\cimv2
    let query = "SELECT TimeGenerated, ProductName, Message, RecordType, ComputerName FROM Win32_ReliabilityRecords";
    
    match wmi_con.raw_query(query) {
        Ok(results) => results,
        Err(e) => {
            eprintln!("Failed to query ReliabilityRecords: {:?}", e);
            vec![]
        }
    }
}
