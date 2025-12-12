# Test Project Structure

This project contains all tests for the DigYourWindows WPF GUI application.

## Directory Organization

- **Unit/**: Unit tests for individual classes and methods
- **Property/**: Property-based tests using FsCheck (minimum 100 iterations)
- **Integration/**: Integration tests for end-to-end functionality

## Running Tests

### Run all tests
```bash
dotnet test
```

### Run with verbose output
```bash
dotnet test --logger "console;verbosity=detailed"
```

### Run specific test class
```bash
dotnet test --filter "FullyQualifiedName~SamplePropertyTests"
```

## Property-Based Testing

Property tests use the `FsCheck` library with xUnit integration. All property tests are configured to run a minimum of 100 iterations.

### Example Property Test
```csharp
[PropertyTest]
public void MyPropertyTest(int x)
{
    Assert.True(x >= int.MinValue);
}
```

### Using Custom Configuration
```csharp
[Property(MaxTest = 100)]
public Property MyPropertyWithConfig(int x)
{
    return (x >= 0 || x < 0).ToProperty();
}
```

## Configuration

- Property test configuration: `FsCheckConfig.cs`
- Test cases per property: 100 (minimum)
- Custom attribute: `[PropertyTest]` applies default configuration

## Test Framework

- **xUnit**: Primary test framework
- **FsCheck**: Property-based testing library
- **FsCheck.Xunit**: Integration between FsCheck and xUnit

## Requirements

Tests validate requirements 2.1, 2.2, 2.3, 2.4, and 2.5 from the specification.
