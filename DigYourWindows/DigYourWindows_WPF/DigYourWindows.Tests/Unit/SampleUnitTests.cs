using Xunit;

namespace DigYourWindows.Tests.Unit;

/// <summary>
/// Sample unit tests to verify xUnit configuration
/// This file demonstrates the basic setup and can be removed once real tests are added
/// </summary>
public class SampleUnitTests
{
    [Fact]
    public void TestBasicAssertion()
    {
        // Basic unit test to verify test infrastructure works
        Assert.Equal(4, 2 + 2);
    }

    [Theory]
    [InlineData(1, 2, 3)]
    [InlineData(0, 0, 0)]
    [InlineData(-1, 1, 0)]
    public void TestParameterizedTest(int a, int b, int expected)
    {
        Assert.Equal(expected, a + b);
    }
}
