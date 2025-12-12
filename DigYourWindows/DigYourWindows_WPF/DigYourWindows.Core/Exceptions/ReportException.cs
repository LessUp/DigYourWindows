namespace DigYourWindows.Core.Exceptions;

/// <summary>
/// Exception thrown when report generation fails.
/// </summary>
public class ReportException : Exception
{
    /// <summary>
    /// The type of report error that occurred.
    /// </summary>
    public ReportErrorType ErrorType { get; }

    /// <summary>
    /// The path where the report was being written (if applicable).
    /// </summary>
    public string? Path { get; }

    /// <summary>
    /// The field that was missing (if applicable).
    /// </summary>
    public string? MissingField { get; }

    public ReportException(string message) : base(message)
    {
        ErrorType = ReportErrorType.Unknown;
    }

    public ReportException(string message, Exception innerException) 
        : base(message, innerException)
    {
        ErrorType = ReportErrorType.Unknown;
    }

    public ReportException(ReportErrorType errorType, string message) 
        : base(message)
    {
        ErrorType = errorType;
    }

    public ReportException(ReportErrorType errorType, string message, Exception inner)
        : base(message, inner)
    {
        ErrorType = errorType;
    }

    private ReportException(
        ReportErrorType errorType, 
        string message, 
        string? path,
        string? missingField) : base(message)
    {
        ErrorType = errorType;
        Path = path;
        MissingField = missingField;
    }

    /// <summary>
    /// Creates a Template exception.
    /// </summary>
    public static ReportException Template(string message) =>
        new(ReportErrorType.Template, $"Template error: {message}");

    /// <summary>
    /// Creates a WriteError exception.
    /// </summary>
    public static ReportException WriteError(string path, string reason) =>
        new(ReportErrorType.WriteError, 
            $"Failed to write report to {path}: {reason}",
            path, null);

    /// <summary>
    /// Creates a Serialization exception.
    /// </summary>
    public static ReportException Serialization(string message) =>
        new(ReportErrorType.Serialization, $"Serialization error: {message}");

    /// <summary>
    /// Creates an InvalidData exception.
    /// </summary>
    public static ReportException InvalidData(string message) =>
        new(ReportErrorType.InvalidData, $"Invalid report data: {message}");

    /// <summary>
    /// Creates a MissingField exception.
    /// </summary>
    public static ReportException MissingField(string field) =>
        new(ReportErrorType.MissingField, 
            $"Missing required field: {field}",
            null, field);
}

/// <summary>
/// Types of report errors that can occur.
/// </summary>
public enum ReportErrorType
{
    Unknown,
    Template,
    WriteError,
    Serialization,
    InvalidData,
    MissingField
}
