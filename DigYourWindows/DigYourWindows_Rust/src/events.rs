use crate::wmi::{get_error_events as wmi_get_error_events};
use crate::wmi_impl::{get_error_events as wmi_impl_get_error_events, analyze_events};
pub use crate::wmi::LogEvent;
pub use crate::wmi_impl::EventAnalysis;



pub fn get_error_events(days: i64) -> Vec<LogEvent> {
    // Try WMI implementation first
    match wmi_impl_get_error_events(days) {
        Ok(events) => events,
        Err(e) => {
            eprintln!("Warning: Failed to get error events from WMI implementation: {:?}. Falling back to mock data.", e);
            
            // Fall back to original implementation
            match wmi_get_error_events(days) {
                Ok(events) => events,
                Err(e2) => {
                    eprintln!("Warning: Failed to get error events from fallback: {:?}", e2);
                    // Final fallback to empty list
                    vec![]
                }
            }
        }
    }
}

/// Analyze events and return analysis results
pub fn get_event_analysis(events: &[LogEvent]) -> EventAnalysis {
    analyze_events(events)
}
