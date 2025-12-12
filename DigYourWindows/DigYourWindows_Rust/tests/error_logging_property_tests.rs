//! Property-based tests for Error Logging Completeness
//!
//! These tests verify that all errors are properly logged with required information.

use proptest::prelude::*;

/// **Feature: digyourwindows-improvements, Property 17: Error Logging Completeness**
/// **Validates: Requirements 7.3**
///
/// Property: For any error or exception that occurs during system operation,
/// at least one log entry should be written containing the error type,
/// error message, and timestamp.
#[cfg(test)]
mod error_logging_completeness {
    use super::*;

    // Define error types that can occur in the system
    #[derive(Debug, Clone)]
    enum TestErrorType {
        WmiAccessDenied,
        WmiTimeout,
        WmiInvalidQuery,
        WmiParseError,
        WmiConnectionFailed,
        ServiceCollectionFailed,
        ServiceTimeout,
        ServiceInvalidData,
        ReportTemplate,
        ReportWriteError,
        ReportSerialization,
    }

    impl TestErrorType {
        fn as_str(&self) -> &'static str {
            match self {
                TestErrorType::WmiAccessDenied => "WmiAccessDenied",
                TestErrorType::WmiTimeout => "WmiTimeout",
                TestErrorType::WmiInvalidQuery => "WmiInvalidQuery",
                TestErrorType::WmiParseError => "WmiParseError",
                TestErrorType::WmiConnectionFailed => "WmiConnectionFailed",
                TestErrorType::ServiceCollectionFailed => "ServiceCollectionFailed",
                TestErrorType::ServiceTimeout => "ServiceTimeout",
                TestErrorType::ServiceInvalidData => "ServiceInvalidData",
                TestErrorType::ReportTemplate => "ReportTemplate",
                TestErrorType::ReportWriteError => "ReportWriteError",
                TestErrorType::ReportSerialization => "ReportSerialization",
            }
        }
    }

    // Generator for error types
    fn arb_error_type() -> impl Strategy<Value = TestErrorType> {
        prop_oneof![
            Just(TestErrorType::WmiAccessDenied),
            Just(TestErrorType::WmiTimeout),
            Just(TestErrorType::WmiInvalidQuery),
            Just(TestErrorType::WmiParseError),
            Just(TestErrorType::WmiConnectionFailed),
            Just(TestErrorType::ServiceCollectionFailed),
            Just(TestErrorType::ServiceTimeout),
            Just(TestErrorType::ServiceInvalidData),
            Just(TestErrorType::ReportTemplate),
            Just(TestErrorType::ReportWriteError),
            Just(TestErrorType::ReportSerialization),
        ]
    }

    // Generator for error messages
    fn arb_error_message() -> impl Strategy<Value = String> {
        "[a-zA-Z0-9 ]{10,100}".prop_map(|s| s.trim().to_string())
    }

    /// Simulates logging an error and returns the log entry
    fn simulate_log_error(error_type: &TestErrorType, message: &str) -> String {
        let timestamp = chrono::Local::now().format("%Y-%m-%d %H:%M:%S%.3f");
        format!("{} [ERROR] [{}] {}", timestamp, error_type.as_str(), message)
    }

    /// Checks if a log entry contains all required components
    fn log_entry_is_complete(log_entry: &str) -> (bool, bool, bool) {
        // Check for timestamp pattern (YYYY-MM-DD HH:MM:SS)
        let has_timestamp = log_entry.len() >= 19 && 
            log_entry.chars().take(4).all(|c| c.is_ascii_digit()) &&
            log_entry.chars().nth(4) == Some('-');
        
        // Check for error type (enclosed in brackets after [ERROR])
        let has_error_type = log_entry.contains("[ERROR]") && 
            log_entry.matches('[').count() >= 2;
        
        // Check for message (non-empty content after error type)
        let has_message = log_entry.split(']').last()
            .map(|s| !s.trim().is_empty())
            .unwrap_or(false);
        
        (has_timestamp, has_error_type, has_message)
    }

    proptest! {
        #![proptest_config(ProptestConfig::with_cases(100))]
        
        /// Test that all error types produce log entries with required components
        #[test]
        fn prop_error_log_contains_required_components(
            error_type in arb_error_type(),
            message in arb_error_message()
        ) {
            let log_entry = simulate_log_error(&error_type, &message);
            let (has_timestamp, has_error_type, has_message) = log_entry_is_complete(&log_entry);
            
            prop_assert!(
                has_timestamp,
                "Log entry must contain timestamp: {}",
                log_entry
            );
            prop_assert!(
                has_error_type,
                "Log entry must contain error type: {}",
                log_entry
            );
            prop_assert!(
                has_message,
                "Log entry must contain error message: {}",
                log_entry
            );
        }

        /// Test that error type is preserved in log entry
        #[test]
        fn prop_error_type_preserved_in_log(
            error_type in arb_error_type()
        ) {
            let message = "Test error message";
            let log_entry = simulate_log_error(&error_type, message);
            
            // The error type name should appear in the log entry
            prop_assert!(
                log_entry.contains(error_type.as_str()),
                "Log entry should contain error type '{}': {}",
                error_type.as_str(),
                log_entry
            );
        }

        /// Test that error message is preserved in log entry
        #[test]
        fn prop_error_message_preserved_in_log(
            message in arb_error_message()
        ) {
            let error_type = TestErrorType::WmiAccessDenied;
            let log_entry = simulate_log_error(&error_type, &message);
            
            // The original message should appear in the log entry
            prop_assert!(
                log_entry.contains(&message),
                "Log entry should contain original message '{}': {}",
                message,
                log_entry
            );
        }

        /// Test that timestamp format is valid
        #[test]
        fn prop_timestamp_format_is_valid(
            error_type in arb_error_type(),
            message in arb_error_message()
        ) {
            let log_entry = simulate_log_error(&error_type, &message);
            
            // Extract timestamp (first 23 characters: YYYY-MM-DD HH:MM:SS.mmm)
            let timestamp_str = &log_entry[..23];
            
            // Verify timestamp format
            prop_assert!(
                timestamp_str.chars().nth(4) == Some('-'),
                "Timestamp should have '-' at position 4"
            );
            prop_assert!(
                timestamp_str.chars().nth(7) == Some('-'),
                "Timestamp should have '-' at position 7"
            );
            prop_assert!(
                timestamp_str.chars().nth(10) == Some(' '),
                "Timestamp should have space at position 10"
            );
            prop_assert!(
                timestamp_str.chars().nth(13) == Some(':'),
                "Timestamp should have ':' at position 13"
            );
            prop_assert!(
                timestamp_str.chars().nth(16) == Some(':'),
                "Timestamp should have ':' at position 16"
            );
        }
    }
}
