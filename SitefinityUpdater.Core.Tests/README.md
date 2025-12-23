# SitefinityContentUpdater.Core Test Suite

This document describes the comprehensive unit and integration tests created for the SitefinityContentUpdater.Core class library.

## Test Coverage Summary

- **Total Tests**: 61
- **Test Categories**: Unit Tests & Integration Tests
- **Test Frameworks**: xUnit, Moq, FluentAssertions

## Test Structure

### 1. RestClient Tests

#### SitefinityConfigTests.cs
Tests for the `SitefinityConfig` configuration class.

**Tests:**
- Property initialization
- Empty initialization
- Property updates

#### RestClientFactoryTests.cs
Tests for the `RestClientFactory` that creates Sitefinity REST clients.

**Tests:**
- Validates null config throws ArgumentNullException
- Ensures URL formatting (appending slash when needed)
- Verifies no duplicate slashes in URLs
- Tests HTTP client creation

### 2. Helper Class Tests

#### ConsoleHelperTests.cs (9 tests)
Tests for console output and user input functionality.

**Tests:**
- `WriteSuccess_ShouldNotThrow` - Validates success messages
- `WriteError_ShouldNotThrow` - Validates error messages
- `WriteInfo_ShouldNotThrow` - Validates info messages
- `WriteWarning_ShouldNotThrow` - Validates warning messages
- `ReadLine_ShouldAcceptPromptWithoutThrowing` - Tests user input
- `Confirm_ShouldReturnTrue_WhenUserEntersY` - Tests confirmation (lowercase)
- `Confirm_ShouldReturnTrue_WhenUserEntersUppercaseY` - Tests confirmation (uppercase)
- `Confirm_ShouldReturnFalse_WhenUserEntersN` - Tests rejection
- `Confirm_ShouldReturnFalse_WhenUserEntersAnythingElse` - Tests invalid input

#### ConfigurationHelperTests.cs (7 tests)
Tests for configuration loading and management.

**Tests:**
- `LoadConfiguration_ShouldReturnIConfiguration` - Tests config loading
- `GetSitefinityConfigAsync_ShouldReturnConfig_WhenAllValuesProvidedInConfiguration` - Tests complete config
- `GetSitefinityConfigAsync_ShouldPromptForMissingValues` - Tests user prompts
- `GetSitefinityConfigAsync_ShouldThrowInvalidOperationException_WhenUrlAndAccessKeyAreEmpty` - Tests validation
- `GetCsvFilePath_ShouldReturnDefaultPath_WhenNotConfigured` - Tests default path
- `GetCsvFilePath_ShouldReturnConfiguredPath_WhenProvided` - Tests custom path
- `GetCsvFilePath_ShouldCombineWithBaseDirectory` - Tests path combination

#### SiteValidatorTests.cs (4 tests)
Tests for the `SiteValidationResult` class.

**Tests:**
- Property initialization
- Default initialization
- Property updates
- Multiple scenario support

#### ContentProcessorTests.cs (6 tests)
Tests for content processing initialization and validation.

**Tests:**
- `ContentProcessor_ShouldThrowArgumentNullException_WhenClientIsNull` - Validates constructor
- `ContentProcessor_ShouldThrowArgumentNullException_WhenCsvFilePathIsNull` - Validates constructor
- `ContentProcessor_ShouldInitialize_WithValidParameters` - Tests valid initialization
- `UpdateContentAsync_ShouldThrowArgumentNullException_WhenContentTypeIsNull` - Parameter validation
- `UpdateContentAsync_ShouldThrowArgumentNullException_WhenFieldNameIsNull` - Parameter validation

### 3. Model/DTO Tests

#### ProcessingResultTests.cs (3 tests)
Tests for batch processing results.

**Tests:**
- Property initialization
- Default initialization
- Accumulation support (for aggregating batch results)

#### ImgDetailTests.cs (3 tests)
Tests for image detail tracking.

**Tests:**
- Property initialization with values
- Null value handling
- Collection operations (filtering by title/ID)

#### ImageMappingTests.cs (10 tests)
Tests for CSV image mapping functionality.

**Tests:**
- Valid GUID parsing
- Null handling for "N/A" string
- Null handling for empty string
- Null handling for whitespace
- Null handling for null value
- Invalid GUID handling
- Case-insensitive "N/A" handling
- Various valid GUID formats (upper/lower case)
- Bulk operations and filtering
- Finding mappings by source ID

### 4. Integration Tests

#### ConfigurationIntegrationTests.cs (3 tests)
Tests for component integration scenarios.

**Tests:**
- `LoadConfiguration_And_GetCsvFilePath_ShouldWorkTogether` - Config and CSV path integration
- `ConfigurationHelper_And_RestClientFactory_ShouldWorkTogether` - Config and client factory integration
- `ConfigurationHelper_ShouldHandleMultipleConfigurations` - Multiple config instances

#### ValidationWorkflowIntegrationTests.cs (4 tests)
Tests for validation workflow logic.

**Tests:**
- `ValidationResult_ShouldPreventProcessing_WhenInvalid` - Invalid state handling
- `ValidationResult_ShouldAllowProcessing_WhenValid` - Valid state handling
- `ValidationResult_ShouldIndicateReconnect_WhenUserChangedSiteId` - Reconnect logic
- `WorkflowStates_ShouldBeDistinct` - State distinction verification

#### CsvMappingIntegrationTests.cs (6 tests)
Tests for CSV mapping functionality.

**Tests:**
- `ImageMapping_ShouldHandleVariousValidFormats` - Multiple GUID formats
- `ImageMapping_ShouldHandleVariousInvalidFormats` - Invalid value handling
- `ImageMapping_ShouldSupportBulkOperations` - Collection operations
- `ImageMapping_ShouldFindMappingBySourceId` - Mapping lookups
- `ImgDetail_ShouldSupportCollectionOperations` - ImgDetail collections
- `ProcessingResult_ShouldAccumulateCorrectly` - Result aggregation

## Test Dependencies

The test project uses the following NuGet packages:

- **xUnit** (2.9.2) - Test framework
- **xUnit.runner.visualstudio** (2.8.2) - Visual Studio test runner
- **Microsoft.NET.Test.Sdk** (17.12.0) - .NET test SDK
- **Moq** (4.20.72) - Mocking framework
- **FluentAssertions** (6.12.2) - Assertion library
- **coverlet.collector** (6.0.2) - Code coverage
- **Microsoft.Extensions.Configuration** (9.0.0) - Configuration support
- **Microsoft.Extensions.Configuration.Json** (9.0.0) - JSON configuration
- **Progress.Sitefinity.RestSdk** (15.4.8622.28) - Sitefinity SDK

## Running the Tests

### Run all tests:
```bash
dotnet test SitefinityContentUpdater.Core.Tests\SitefinityContentUpdater.Core.Tests.csproj
```

### Run with verbose output:
```bash
dotnet test SitefinityContentUpdater.Core.Tests\SitefinityContentUpdater.Core.Tests.csproj --verbosity normal
```

### Run specific test class:
```bash
dotnet test --filter "FullyQualifiedName~ConsoleHelperTests"
```

### Generate code coverage:
```bash
dotnet test SitefinityContentUpdater.Core.Tests\SitefinityContentUpdater.Core.Tests.csproj --collect:"XPlat Code Coverage"
```

## Test Design Principles

1. **Isolation**: Each test is independent and doesn't rely on external state
2. **Console I/O Management**: Tests that interact with Console use try-finally blocks to properly redirect and restore I/O streams, preventing `ObjectDisposedException`
3. **Focused Tests**: Each test validates one specific behavior
4. **Clear Naming**: Test names follow the pattern `MethodName_Should_ExpectedBehavior_When_Condition`
5. **Comprehensive Coverage**: Tests cover happy paths, error cases, and edge cases
6. **No External Dependencies**: Tests don't require a live Sitefinity instance

## Important Patterns

### Console I/O Redirection Pattern
When testing code that writes to Console, always use try-finally to restore the original streams:

```csharp
[Fact]
public void SomeTest()
{
    var originalOut = Console.Out;
    var originalIn = Console.In;  // if you also redirect input
    try
    {
        using var sw = new StringWriter();
        using var sr = new StringReader("input\n");  // if needed
        Console.SetOut(sw);
        Console.SetIn(sr);  // if needed
        
        // Your test code here
    }
    finally
    {
        Console.SetOut(originalOut);
        Console.SetIn(originalIn);  // if you redirected input
    }
}
```

This pattern prevents `ObjectDisposedException` by ensuring Console streams are always restored even if the test fails or the StringWriter is disposed.

## Test Categories

- **Unit Tests**: Test individual components in isolation (RestClient, Helpers, Models)
- **Integration Tests**: Test interactions between multiple components (Configuration + RestClient, Validation workflow)

## Future Enhancements

Potential areas for additional testing:

1. **End-to-End Tests**: Tests with a mock Sitefinity instance
2. **Performance Tests**: Measure performance of batch operations
3. **CSV File I/O Tests**: Test actual CSV file reading/writing
4. **HTML Parsing Tests**: Test AngleSharp integration for image replacement
5. **Error Recovery Tests**: Test resilience and retry logic
6. **Concurrent Operation Tests**: Test thread-safety of operations
