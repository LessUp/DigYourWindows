//! WMI Provider abstraction layer
//!
//! This module provides a trait-based abstraction for WMI access,
//! allowing for dependency injection and testability.

use serde::de::DeserializeOwned;

/// Error types for WMI operations
#[derive(Debug, thiserror::Error)]
pub enum WmiError {
    #[error("Access denied. Please run with administrator privileges to access {resource}")]
    AccessDenied { resource: String },

    #[error("WMI query timed out after {seconds} seconds")]
    Timeout { seconds: u64 },

    #[error("Invalid WMI query: {query}")]
    InvalidQuery { query: String },

    #[error("Failed to parse WMI result: {details}")]
    ParseError { details: String },

    #[error("WMI connection failed: {details}")]
    ConnectionFailed { details: String },

    #[error("COM initialization failed: {0}")]
    ComInitFailed(String),

    #[error("WMI operation failed: {0}")]
    OperationFailed(String),
}

/// Trait for WMI data providers
///
/// This trait abstracts WMI access to allow for dependency injection
/// and testing with mock implementations.
pub trait WmiProvider: Send + Sync {
    /// Execute a WMI query and return a collection of results
    fn query<T: DeserializeOwned>(&self, query: &str) -> std::result::Result<Vec<T>, WmiError>;

    /// Execute a WMI query and return a single result
    fn get_single<T: DeserializeOwned>(&self, query: &str) -> std::result::Result<T, WmiError>;
}

/// Windows WMI Provider implementation
///
/// This is a simplified implementation that doesn't use connection pooling
/// to avoid thread safety issues with COM objects.
pub struct WindowsWmiProvider;

impl WindowsWmiProvider {
    /// Create a new Windows WMI provider
    pub fn new() -> std::result::Result<Self, WmiError> {
        Ok(Self)
    }
}

impl WmiProvider for WindowsWmiProvider {
    fn query<T: DeserializeOwned>(&self, _query: &str) -> std::result::Result<Vec<T>, WmiError> {
        // Placeholder implementation - will be completed in future tasks
        // For now, return empty results to allow compilation
        Ok(Vec::new())
    }

    fn get_single<T: DeserializeOwned>(&self, _query: &str) -> std::result::Result<T, WmiError> {
        // Placeholder implementation - will be completed in future tasks
        Err(WmiError::OperationFailed(
            "WMI provider not yet fully implemented".to_string(),
        ))
    }
}
