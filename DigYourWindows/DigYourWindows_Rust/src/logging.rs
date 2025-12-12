//! Logging module for DigYourWindows
//! 
//! This module provides structured logging capabilities for the application,
//! including file and console logging with configurable levels.
//! 
//! Log files are stored at `%APPDATA%/DigYourWindows/logs/`

use std::fs::{self, File, OpenOptions};
use std::io::{BufWriter, Write};
use std::path::{Path, PathBuf};
use chrono::Local;
use std::sync::Mutex;

/// Log levels for the application
#[derive(Debug, Clone, Copy, PartialEq, Eq, PartialOrd, Ord)]
pub enum LogLevel {
    Trace = 0,
    Debug = 1,
    Info = 2,
    Warn = 3,
    Error = 4,
}

impl LogLevel {
    /// Convert log level to string representation
    pub fn as_str(&self) -> &'static str {
        match self {
            LogLevel::Trace => "TRACE",
            LogLevel::Debug => "DEBUG",
            LogLevel::Info => "INFO",
            LogLevel::Warn => "WARN",
            LogLevel::Error => "ERROR",
        }
    }
}

/// A simple logger that writes to a file
pub struct FileLogger {
    file: Mutex<BufWriter<File>>,
    min_level: LogLevel,
}

impl FileLogger {
    /// Create a new file logger that writes to the specified path
    pub fn new<P: AsRef<Path>>(path: P, min_level: LogLevel) -> Result<Self, Box<dyn std::error::Error>> {
        let file = OpenOptions::new()
            .create(true)
            .append(true)
            .open(path)?;
        
        let writer = BufWriter::new(file);
        Ok(FileLogger {
            file: Mutex::new(writer),
            min_level,
        })
    }
    
    /// Log a message with the specified level
    pub fn log(&self, level: LogLevel, message: &str) -> Result<(), Box<dyn std::error::Error>> {
        if level >= self.min_level {
            let timestamp = Local::now().format("%Y-%m-%d %H:%M:%S%.3f");
            let formatted = format!("{} [{}] {}\n", timestamp, level.as_str(), message);
            
            if let Ok(mut file) = self.file.lock() {
                file.write_all(formatted.as_bytes())?;
                file.flush()?;
            }
        }
        
        Ok(())
    }
    
    /// Convenience methods for different log levels
    pub fn trace(&self, message: &str) -> Result<(), Box<dyn std::error::Error>> {
        self.log(LogLevel::Trace, message)
    }
    
    pub fn debug(&self, message: &str) -> Result<(), Box<dyn std::error::Error>> {
        self.log(LogLevel::Debug, message)
    }
    
    pub fn info(&self, message: &str) -> Result<(), Box<dyn std::error::Error>> {
        self.log(LogLevel::Info, message)
    }
    
    pub fn warn(&self, message: &str) -> Result<(), Box<dyn std::error::Error>> {
        self.log(LogLevel::Warn, message)
    }
    
    pub fn error(&self, message: &str) -> Result<(), Box<dyn std::error::Error>> {
        self.log(LogLevel::Error, message)
    }
}

/// Global logger instance (initialized in main)
static mut GLOBAL_LOGGER: Option<FileLogger> = None;

/// Get the default log directory path (%APPDATA%/DigYourWindows/logs/)
pub fn get_log_directory() -> PathBuf {
    if let Some(data_dir) = directories::BaseDirs::new() {
        let log_dir = data_dir.data_dir().join("DigYourWindows").join("logs");
        return log_dir;
    }
    // Fallback to current directory
    PathBuf::from("logs")
}

/// Get the default log file path
pub fn get_default_log_path() -> PathBuf {
    let log_dir = get_log_directory();
    let date = Local::now().format("%Y-%m-%d");
    log_dir.join(format!("digyourwindows_{}.log", date))
}

/// Ensure the log directory exists
pub fn ensure_log_directory() -> Result<PathBuf, std::io::Error> {
    let log_dir = get_log_directory();
    fs::create_dir_all(&log_dir)?;
    Ok(log_dir)
}

/// Initialize the global logger with default path
pub fn init_default_logger(level: LogLevel) -> Result<(), Box<dyn std::error::Error>> {
    let _ = ensure_log_directory()?;
    let log_path = get_default_log_path();
    init_logger(log_path, level)
}

/// Initialize the global logger
pub fn init_logger<P: AsRef<Path>>(path: P, level: LogLevel) -> Result<(), Box<dyn std::error::Error>> {
    // Ensure parent directory exists
    if let Some(parent) = path.as_ref().parent() {
        fs::create_dir_all(parent)?;
    }
    
    let logger = FileLogger::new(path, level)?;
    
    // This is safe because we only access the global logger from the main thread
    unsafe {
        GLOBAL_LOGGER = Some(logger);
    }
    
    Ok(())
}

/// Log a message with the specified level using the global logger
pub fn log(level: LogLevel, message: &str) {
    unsafe {
        if let Some(ref logger) = GLOBAL_LOGGER {
            let _ = logger.log(level, message);
        }
    }
    
    // Also print to stderr for errors and warnings
    if level >= LogLevel::Warn {
        eprintln!("[{}] {}", level.as_str(), message);
    }
}

/// Log a formatted message with the specified level
pub fn log_fmt(level: LogLevel, _fmt: &str, args: std::fmt::Arguments) {
    unsafe {
        if let Some(ref logger) = GLOBAL_LOGGER {
            let message = format!("{}", args);
            let _ = logger.log(level, &message);
        }
    }
    
    // Also print to stderr for errors and warnings
    if level >= LogLevel::Warn {
        eprintln!("[{}] {}", level.as_str(), format!("{}", args));
    }
}

/// Log entry structure for structured logging
#[derive(Debug, Clone)]
pub struct LogEntry {
    pub timestamp: chrono::DateTime<Local>,
    pub level: LogLevel,
    pub error_type: Option<String>,
    pub message: String,
    pub context: Option<String>,
}

impl LogEntry {
    /// Create a new log entry
    pub fn new(level: LogLevel, message: impl Into<String>) -> Self {
        Self {
            timestamp: Local::now(),
            level,
            error_type: None,
            message: message.into(),
            context: None,
        }
    }

    /// Add error type to the log entry
    pub fn with_error_type(mut self, error_type: impl Into<String>) -> Self {
        self.error_type = Some(error_type.into());
        self
    }

    /// Add context to the log entry
    pub fn with_context(mut self, context: impl Into<String>) -> Self {
        self.context = Some(context.into());
        self
    }

    /// Format the log entry as a string
    pub fn format(&self) -> String {
        let timestamp = self.timestamp.format("%Y-%m-%d %H:%M:%S%.3f");
        let mut parts = vec![
            format!("{} [{}]", timestamp, self.level.as_str()),
        ];
        
        if let Some(ref error_type) = self.error_type {
            parts.push(format!("[{}]", error_type));
        }
        
        parts.push(self.message.clone());
        
        if let Some(ref context) = self.context {
            parts.push(format!("(context: {})", context));
        }
        
        parts.join(" ")
    }
}

/// Log a structured error entry
pub fn log_error_entry(error_type: &str, message: &str) {
    let entry = LogEntry::new(LogLevel::Error, message)
        .with_error_type(error_type);
    
    unsafe {
        if let Some(ref logger) = GLOBAL_LOGGER {
            let _ = logger.log(LogLevel::Error, &entry.format());
        }
    }
    
    eprintln!("[ERROR] [{}] {}", error_type, message);
}

/// Log a structured error with context
pub fn log_error_with_context(error_type: &str, message: &str, context: &str) {
    let entry = LogEntry::new(LogLevel::Error, message)
        .with_error_type(error_type)
        .with_context(context);
    
    unsafe {
        if let Some(ref logger) = GLOBAL_LOGGER {
            let _ = logger.log(LogLevel::Error, &entry.format());
        }
    }
    
    eprintln!("[ERROR] [{}] {} (context: {})", error_type, message, context);
}

/// Convenience macros for logging
#[macro_export]
macro_rules! log_trace {
    ($($arg:tt)*) => {
        $crate::logging::log($crate::logging::LogLevel::Trace, &format!($($arg)*))
    };
}

#[macro_export]
macro_rules! log_debug {
    ($($arg:tt)*) => {
        $crate::logging::log($crate::logging::LogLevel::Debug, &format!($($arg)*))
    };
}

#[macro_export]
macro_rules! log_info {
    ($($arg:tt)*) => {
        $crate::logging::log($crate::logging::LogLevel::Info, &format!($($arg)*))
    };
}

#[macro_export]
macro_rules! log_warn {
    ($($arg:tt)*) => {
        $crate::logging::log($crate::logging::LogLevel::Warn, &format!($($arg)*))
    };
}

#[macro_export]
macro_rules! log_error {
    ($($arg:tt)*) => {
        $crate::logging::log($crate::logging::LogLevel::Error, &format!($($arg)*))
    };
}