use crate::wmi::{get_reliability_records as wmi_get_reliability_records};
use crate::wmi_impl::{get_reliability_records as wmi_impl_get_reliability_records};
pub use crate::wmi::ReliabilityRecord;



pub fn get_reliability_records() -> Vec<ReliabilityRecord> {
    // Try WMI implementation first
    match wmi_impl_get_reliability_records() {
        Ok(records) => records,
        Err(e) => {
            eprintln!("Warning: Failed to get reliability records from WMI implementation: {:?}. Falling back to mock data.", e);
            
            // Fall back to original implementation
            match wmi_get_reliability_records() {
                Ok(records) => records,
                Err(e2) => {
                    eprintln!("Warning: Failed to get reliability records from fallback: {:?}", e2);
                    // Final fallback to empty list
                    vec![]
                }
            }
        }
    }
}
