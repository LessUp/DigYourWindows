namespace DigYourWindows.Core.Exceptions;

/// <summary>
/// Exception thrown when service layer operations fail.
/// </summary>
public class ServiceException : Exception
{
    /// <summary>
    /// The type of service error that occurred.
    /// </summary>
    public ServiceErrorType ErrorType { get; }

    /// <summary>
    /// The name of the service that failed.
    /// </summary>
    public string? ServiceName { get; }

    /// <summary>
    /// List of services that succeeded (for partial collection errors).
    /// </summary>
    public IReadOnlyList<string>? SuccessfulServices { get; }

    /// <summary>
    /// List of services that failed (for partial collection errors).
    /// </summary>
    public IReadOnlyList<string>? FailedServices { get; }

    public ServiceException(string message) : base(message)
    {
        ErrorType = ServiceErrorType.Unknown;
    }

    public ServiceException(string message, Exception innerException) 
        : base(message, innerException)
    {
        ErrorType = ServiceErrorType.Unknown;
    }

    public ServiceException(ServiceErrorType errorType, string message) 
        : base(message)
    {
        ErrorType = errorType;
    }

    public ServiceException(ServiceErrorType errorType, string message, Exception inner)
        : base(message, inner)
    {
        ErrorType = errorType;
    }

    private ServiceException(
        ServiceErrorType errorType, 
        string message, 
        string? serviceName,
        IReadOnlyList<string>? successful,
        IReadOnlyList<string>? failed) : base(message)
    {
        ErrorType = errorType;
        ServiceName = serviceName;
        SuccessfulServices = successful;
        FailedServices = failed;
    }

    /// <summary>
    /// Creates a CollectionFailed exception.
    /// </summary>
    public static ServiceException CollectionFailed(string service, string reason) =>
        new(ServiceErrorType.CollectionFailed, 
            $"Failed to collect {service} data: {reason}",
            service, null, null);

    /// <summary>
    /// Creates a PartialCollection exception.
    /// </summary>
    public static ServiceException PartialCollection(
        IReadOnlyList<string> successful, 
        IReadOnlyList<string> failed) =>
        new(ServiceErrorType.PartialCollection,
            $"Partial data collection: [{string.Join(", ", successful)}] succeeded, [{string.Join(", ", failed)}] failed",
            null, successful, failed);

    /// <summary>
    /// Creates an InvalidData exception.
    /// </summary>
    public static ServiceException InvalidData(string source, string details) =>
        new(ServiceErrorType.InvalidData, 
            $"Invalid data from {source}: {details}",
            source, null, null);

    /// <summary>
    /// Creates a Timeout exception.
    /// </summary>
    public static ServiceException Timeout(string service, int seconds) =>
        new(ServiceErrorType.Timeout,
            $"Service timeout after {seconds} seconds while collecting {service} data",
            service, null, null);

    /// <summary>
    /// Creates an AccessDenied exception.
    /// </summary>
    public static ServiceException AccessDenied(string resource) =>
        new(ServiceErrorType.AccessDenied,
            $"Access denied to {resource}. Please run with administrator privileges",
            resource, null, null);
}

/// <summary>
/// Types of service errors that can occur.
/// </summary>
public enum ServiceErrorType
{
    Unknown,
    CollectionFailed,
    PartialCollection,
    InvalidData,
    Timeout,
    AccessDenied,
    WmiError
}
