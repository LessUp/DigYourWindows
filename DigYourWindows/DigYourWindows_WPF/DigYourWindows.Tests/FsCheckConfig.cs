using FsCheck;
using FsCheck.Xunit;

namespace DigYourWindows.Tests;

/// <summary>
/// FsCheck configuration for property-based tests
/// Ensures minimum 100 iterations per test as per requirements
/// </summary>
public static class FsCheckConfig
{
    /// <summary>
    /// Default configuration with 100 test cases minimum
    /// </summary>
    public static Configuration Default => new Configuration { MaxNbOfTest = 100, QuietOnSuccess = true };

    /// <summary>
    /// Verbose configuration for debugging
    /// </summary>
    public static Configuration Verbose => new Configuration { MaxNbOfTest = 100, QuietOnSuccess = false };
}

/// <summary>
/// Custom PropertyAttribute that applies our configuration
/// Use this instead of [Property] to ensure 100 iterations
/// </summary>
public class PropertyTestAttribute : PropertyAttribute
{
    public PropertyTestAttribute()
    {
        MaxTest = 100;
        QuietOnSuccess = true;
    }
}
