//! Error types for DigYourWindows
//!
//! This module defines a comprehensive error hierarchy for the application,
//! providing specific error types for different failure scenarios.

use crate::wmi_provider::WmiError;

/// Top-level diagnostic error type
///
/// This enum encompasses all possible errors that can occur during
/// diagnostic data collection and report generation.
#[derive(Debug, thiserror::Error)]
pub enum DiagnosticError {
    #[error("WMI error: {0}")]
    Wmi(#[from] WmiError),

    #[error("Service error: {0}")]
    Service(#[from] ServiceError),

    #[error("Report generation error: {0}")]
    Report(#[from] ReportError),

    #[error("IO error: {0}")]
    Io(#[from] std::io::Error),

    #[error("Configuration error: {0}")]
    Config(String),
}

/// Service layer errors
///
/// Errors that occur during data collection from various system services.
#[derive(Debug, thiserror::Error)]
pub enum ServiceError {
    #[error("Failed to collect {service} data: {reason}")]
    CollectionFailed { service: String, reason: String },

    #[error("Partial data collection: {successful:?} succeeded, {failed:?} failed")]
    PartialCollection {
        successful: Vec<String>,
        failed: Vec<String>,
    },

    #[error("Invalid data from {data_source}: {details}")]
    InvalidData { data_source: String, details: String },

    #[error("Service timeout after {seconds} seconds while collecting {service} data")]
    Timeout { service: String, seconds: u64 },

    #[error("Access denied to {resource}. Please run with administrator privileges")]
    AccessDenied { resource: String },

    #[error("WMI error in {service}: {details}")]
    WmiError { service: String, details: String },
}

/// Report generation errors
///
/// Errors that occur during report generation (HTML, JSON, etc.)
#[derive(Debug, thiserror::Error)]
pub enum ReportError {
    #[error("Template error: {0}")]
    Template(String),

    #[error("Failed to write report to {path}: {reason}")]
    WriteError { path: String, reason: String },

    #[error("Serialization error: {0}")]
    Serialization(String),

    #[error("Invalid report data: {0}")]
    InvalidData(String),

    #[error("Missing required field: {0}")]
    MissingField(String),
}

impl ServiceError {
    /// Create a new CollectionFailed error
    pub fn collection_failed(service: impl Into<String>, reason: impl Into<String>) -> Self {
        ServiceError::CollectionFailed {
            service: service.into(),
            reason: reason.into(),
        }
    }

    /// Create a new InvalidData error
    pub fn invalid_data(data_source: impl Into<String>, details: impl Into<String>) -> Self {
        ServiceError::InvalidData {
            data_source: data_source.into(),
            details: details.into(),
        }
    }

    /// Create a new Timeout error
    pub fn timeout(service: impl Into<String>, seconds: u64) -> Self {
        ServiceError::Timeout {
            service: service.into(),
            seconds,
        }
    }

    /// Create a new AccessDenied error
    pub fn access_denied(resource: impl Into<String>) -> Self {
        ServiceError::AccessDenied {
            resource: resource.into(),
        }
    }

    /// Create a new WmiError
    pub fn wmi_error(service: impl Into<String>, details: impl Into<String>) -> Self {
        ServiceError::WmiError {
            service: service.into(),
            details: details.into(),
        }
    }
}

impl ReportError {
    /// Create a new Template error
    pub fn template(message: impl Into<String>) -> Self {
        ReportError::Template(message.into())
    }

    /// Create a new WriteError
    pub fn write_error(path: impl Into<String>, reason: impl Into<String>) -> Self {
        ReportError::WriteError {
            path: path.into(),
            reason: reason.into(),
        }
    }

    /// Create a new Serialization error
    pub fn serialization(message: impl Into<String>) -> Self {
        ReportError::Serialization(message.into())
    }

    /// Create a new InvalidData error
    pub fn invalid_data(message: impl Into<String>) -> Self {
        ReportError::InvalidData(message.into())
    }

    /// Create a new MissingField error
    pub fn missing_field(field: impl Into<String>) -> Self {
        ReportError::MissingField(field.into())
    }
}

/// Result type alias for DiagnosticError
pub type DiagnosticResult<T> = Result<T, DiagnosticError>;

/// Result type alias for ServiceError
pub type ServiceResult<T> = Result<T, ServiceError>;

/// Result type alias for ReportError
pub type ReportResult<T> = Result<T, ReportError>;
