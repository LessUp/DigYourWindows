namespace DigYourWindows.Core.Exceptions;

/// <summary>
/// Exception thrown when WMI operations fail.
/// </summary>
public class WmiException : Exception
{
    /// <summary>
    /// The type of WMI error that occurred.
    /// </summary>
    public WmiErrorType ErrorType { get; }

    /// <summary>
    /// The resource that was being accessed when the error occurred.
    /// </summary>
    public string? Resource { get; }

    /// <summary>
    /// The WMI query that was being executed when the error occurred.
    /// </summary>
    public string? Query { get; private set; }

    public WmiException(string message) : base(message)
    {
        ErrorType = WmiErrorType.Unknown;
    }

    public WmiException(string message, Exception innerException) 
        : base(message, innerException)
    {
        ErrorType = WmiErrorType.Unknown;
    }

    public WmiException(WmiErrorType errorType, string message) : base(message)
    {
        ErrorType = errorType;
    }

    public WmiException(WmiErrorType errorType, string message, Exception inner)
        : base(message, inner)
    {
        ErrorType = errorType;
    }

    public WmiException(WmiErrorType errorType, string message, string? resource)
        : base(message)
    {
        ErrorType = errorType;
        Resource = resource;
    }

    /// <summary>
    /// Creates an AccessDenied exception.
    /// </summary>
    public static WmiException AccessDenied(string resource) =>
        new(WmiErrorType.AccessDenied, 
            $"Access denied. Please run with administrator privileges to access {resource}",
            resource);

    /// <summary>
    /// Creates a Timeout exception.
    /// </summary>
    public static WmiException Timeout(int seconds) =>
        new(WmiErrorType.Timeout, $"WMI query timed out after {seconds} seconds");

    /// <summary>
    /// Creates an InvalidQuery exception.
    /// </summary>
    public static WmiException InvalidQuery(string query) =>
        new(WmiErrorType.InvalidQuery, $"Invalid WMI query: {query}") { Query = query };

    /// <summary>
    /// Creates a ParseError exception.
    /// </summary>
    public static WmiException ParseError(string details) =>
        new(WmiErrorType.ParseError, $"Failed to parse WMI result: {details}");

    /// <summary>
    /// Creates a ConnectionFailed exception.
    /// </summary>
    public static WmiException ConnectionFailed(string details) =>
        new(WmiErrorType.ConnectionFailed, $"WMI connection failed: {details}");
}

/// <summary>
/// Types of WMI errors that can occur.
/// </summary>
public enum WmiErrorType
{
    Unknown,
    AccessDenied,
    Timeout,
    InvalidQuery,
    ParseError,
    ConnectionFailed,
    ComInitFailed,
    OperationFailed
}
