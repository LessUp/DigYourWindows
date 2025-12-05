//! Logging module for DigYourWindows
//! 
//! This module provides structured logging capabilities for the application

use std::fs::{File, OpenOptions};
use std::io::{BufWriter, Write};
use std::path::Path;
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

/// Initialize the global logger
pub fn init_logger<P: AsRef<Path>>(path: P, level: LogLevel) -> Result<(), Box<dyn std::error::Error>> {
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
pub fn log_fmt(level: LogLevel, fmt: &str, args: std::fmt::Arguments) {
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