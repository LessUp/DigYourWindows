using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace DigYourWindows.Tests.Property;

/// <summary>
/// Sample property tests to verify FsCheck configuration
/// This file demonstrates the basic setup and can be removed once real tests are added
/// </summary>
public class SamplePropertyTests
{
    [PropertyTest]
    public void TestFsCheckConfiguration(int x, int y)
    {
        // This test verifies that FsCheck is configured correctly
        // and runs the minimum 100 iterations
        
        // Property: addition is commutative
        Assert.Equal(x + y, y + x);
    }

    [PropertyTest]
    public FsCheck.Property TestWithProperty(int x)
    {
        // Alternative syntax using FsCheck's Property type
        return (x >= 0 || x < 0).ToProperty();
    }
}
