# SitefinityContentUpdater.Core

A .NET 9 class library providing core functionality for Sitefinity CMS content manipulation and relationship management via the REST API.

## Overview

This library serves as the foundation for Sitefinity content management tools, providing reusable components for:
- REST API client management
- Content item batch processing
- Relationship creation and management
- CSV-based data mapping
- Configuration management
- Site validation
- Console output utilities

## Components

### Helpers

#### ConfigurationHelper
Manages application configuration loading and CSV file path resolution.

**Key Methods:**
- `LoadConfiguration()`: Loads configuration from `appsettings.json`
- `GetSitefinityConfigAsync(IConfiguration)`: Retrieves Sitefinity connection settings
- `GetCsvFilePath(IConfiguration, string)`: Resolves CSV file paths

**Usage:**
```csharp
var config = ConfigurationHelper.LoadConfiguration();
var sitefinityConfig = await ConfigurationHelper.GetSitefinityConfigAsync(config);
var csvPath = ConfigurationHelper.GetCsvFilePath(config);
```

#### ConsoleHelper
Provides color-coded console output for better user experience.

**Key Methods:**
- `WriteSuccess(string)`: Green output for success messages
- `WriteError(string)`: Red output for errors
- `WriteInfo(string)`: Cyan output for informational messages
- `WriteWarning(string)`: Yellow output for warnings
- `ReadLine(string)`: Prompts for user input
- `Confirm(string)`: Yes/no confirmation prompt

**Usage:**
```csharp
ConsoleHelper.WriteSuccess("Operation completed successfully!");
ConsoleHelper.WriteError("An error occurred");
ConsoleHelper.WriteInfo("Processing 50 items...");
ConsoleHelper.WriteWarning("Item skipped - no images found");

var proceed = ConsoleHelper.Confirm("Continue with operation? (y/n)");
```

#### ContentProcessor
Handles batch processing of content items with HTML content and image reference updates.

**Key Features:**
- Batch processing (50 items per batch)
- CSV-based image ID mapping
- Smart image matching strategies
- HTML parsing and manipulation using AngleSharp
- Concurrent image metadata retrieval

**Key Methods:**
- `UpdateContentAsync(string, string, bool)`: Main entry point for content updates
- Test mode support for single-item validation
- Comprehensive error handling and logging

**CSV Format:**
```csv
Image Title,Source Id,Target Id
My Image,f9c59b18-eaf3-4813-893b-307a1eddd46a,a1b2c3d4-e5f6-7890-abcd-ef1234567890
```

**Usage:**
```csharp
var processor = new ContentProcessor(restClient, csvFilePath);
var result = await processor.UpdateContentAsync("newsitems", "Content", testMode: true);
```

#### RelationshipProcessor
Handles batch creation of relationships between content items.

**Key Features:**
- CSV-based relationship mapping
- Batch processing (50 items per batch)
- Concurrent relationship creation
- Multiple ID separator support (comma, semicolon, pipe)
- Test mode for validation

**Key Methods:**
- `BuildRelationshipsAsync(string, string, bool)`: Main entry point for relationship building
- `ParseRelatedItemIds(string)`: Parses comma/semicolon/pipe-separated GUIDs
- `BatchRelateItemsAsync()`: Processes relationships in batches with concurrent execution

**CSV Format:**
```csv
ParentItemId,RelatedContentType,RelatedItemIds
f9c59b18-eaf3-4813-893b-307a1eddd46a,newsitems,a1b2c3d4-e5f6-7890-abcd-ef1234567890;b2c3d4e5-f6a7-8901-bcde-f23456789012
```

**Usage:**
```csharp
var processor = new RelationshipProcessor(restClient, csvFilePath);
var result = await processor.BuildRelationshipsAsync("newsitems", "RelatedNews", testMode: true);
```

#### SiteValidator
Validates Sitefinity site connections and provides user confirmation workflows.

**Key Methods:**
- `ValidateAndConfirmSiteAsync(IRestClient, Guid)`: Validates site connection and prompts for confirmation

**Usage:**
```csharp
var validator = new SiteValidator();
var validationResult = await validator.ValidateAndConfirmSiteAsync(client, siteId);

if (!validationResult.IsValid)
{
    return;
}

if (validationResult.UserChangedSiteId)
{
    // Reconnect with new site ID
}
```

### REST Client

#### RestClientFactory
Creates and configures Sitefinity REST API clients.

**Key Methods:**
- `GetRestClient(SitefinityConfig)`: Creates configured REST client instance

**Usage:**
```csharp
var config = new SitefinityConfig
{
    Url = "https://site.com/sf/system/",
    AccessKey = "base64-encoded-key",
    SiteId = Guid.Parse("site-id-guid")
};

var client = await RestClientFactory.GetRestClient(config);
```

#### SitefinityConfig
Configuration model for Sitefinity connection settings.

**Properties:**
- `Url`: REST API endpoint URL
- `AccessKey`: Base64-encoded access key
- `SiteId`: Site GUID

### Extensions

#### RestClientExtensions
Extension methods for REST client operations (currently empty, reserved for future extensions).

## Models

### RelationshipMapping
Represents a relationship mapping from CSV.

**Properties:**
- `ParentItemId`: GUID of parent content item
- `RelatedContentType`: Content type of related items
- `RelatedItemIds`: Comma/semicolon/pipe-separated related item GUIDs

### ImageMapping
Represents an image ID mapping from CSV (used in ContentProcessor).

**Properties:**
- `ImageTitle`: Title of the image
- `SourceId`: Source image GUID
- `TargetId`: Target image GUID (nullable)

### ImgDetail
Internal model for tracking image details during processing.

**Properties:**
- `ImageTitle`: Title from HTML alt/title attributes
- `ImageId`: Extracted or mapped image ID

### ProcessingResult
Tracks batch processing results.

**Properties:**
- `TotalProcessed`: Count of items processed
- `TotalUpdated`: Count of items successfully updated
- `TotalSkipped`: Count of items skipped

### SiteValidationResult
Result of site validation workflow.

**Properties:**
- `IsValid`: Whether validation succeeded
- `UserChangedSiteId`: Whether user changed site ID
- `NewSiteId`: New site ID if changed

## Dependencies

### Required NuGet Packages

```xml
<PackageReference Include="AngleSharp" Version="1.4.0" />
<PackageReference Include="CsvHelper" Version="33.1.0" />
<PackageReference Include="Microsoft.Extensions.Configuration" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="9.0.0" />
<PackageReference Include="Progress.Sitefinity.RestSdk" Version="15.4.8622.28" />
```

## Usage Examples

### Basic Content Update Workflow

```csharp
// 1. Load configuration
var config = ConfigurationHelper.LoadConfiguration();
var sitefinityConfig = await ConfigurationHelper.GetSitefinityConfigAsync(config);
var csvPath = ConfigurationHelper.GetCsvFilePath(config);

// 2. Create REST client
var client = await RestClientFactory.GetRestClient(sitefinityConfig);

// 3. Validate site
var validator = new SiteValidator();
var validationResult = await validator.ValidateAndConfirmSiteAsync(client, sitefinityConfig.SiteId);

if (!validationResult.IsValid)
{
    return;
}

// 4. Process content
var processor = new ContentProcessor(client, csvPath);
var result = await processor.UpdateContentAsync("newsitems", "Content", testMode: true);

ConsoleHelper.WriteSuccess($"Content update completed: {result}");
```

### Basic Relationship Building Workflow

```csharp
// 1. Load configuration
var config = ConfigurationHelper.LoadConfiguration();
var sitefinityConfig = await ConfigurationHelper.GetSitefinityConfigAsync(config);
var csvPath = ConfigurationHelper.GetCsvFilePath(config, "RelationshipCsvFilePath");

// 2. Create REST client
var client = await RestClientFactory.GetRestClient(sitefinityConfig);

// 3. Validate site
var validator = new SiteValidator();
var validationResult = await validator.ValidateAndConfirmSiteAsync(client, sitefinityConfig.SiteId);

if (!validationResult.IsValid)
{
    return;
}

// 4. Build relationships
var processor = new RelationshipProcessor(client, csvPath);
var result = await processor.BuildRelationshipsAsync("newsitems", "RelatedNews", testMode: true);

ConsoleHelper.WriteSuccess($"Relationship building completed: {result}");
```

## Error Handling

All components include comprehensive error handling:

- **Null checks**: Arguments are validated with `ArgumentNullException`
- **File validation**: CSV files are checked for existence
- **API errors**: REST API errors are caught and logged
- **CSV parsing errors**: Invalid data is logged with warnings
- **Batch failures**: Individual failures don't stop batch processing

## Performance Characteristics

### Batch Processing
- **Batch Size**: 50 items per batch (configurable constant)
- **Concurrency**: Concurrent operations within batches using `Task.WhenAll`
- **Sequential Batches**: Batches processed sequentially to avoid API overload

### CSV Operations
- **Loading**: CSV files loaded once per batch
- **Parsing**: Efficient parsing using CsvHelper library
- **Validation**: GUID validation with graceful error handling

### Memory Management
- **Streaming**: CSV reading uses streaming for large files
- **Batch Processing**: Limited memory footprint per batch
- **Async Operations**: Non-blocking I/O throughout

## Testing

The library has comprehensive test coverage (84 tests):

- **Unit Tests**: Individual component testing with mocked dependencies
- **Integration Tests**: Multi-component workflow testing
- **CSV Tests**: CSV parsing and validation
- **Error Handling Tests**: Exception and edge case coverage

See [../SitefinityUpdater.Core.Tests/README.md](../SitefinityUpdater.Core.Tests/README.md) for detailed test documentation.

## Best Practices

### Using the Library

1. **Always validate configuration** before processing
2. **Use test mode** for initial validation
3. **Handle validation results** appropriately
4. **Log operations** using ConsoleHelper for user feedback
5. **Process in batches** to avoid memory and API issues

### CSV File Management

1. **Validate GUIDs** before processing
2. **Handle missing mappings** gracefully
3. **Use consistent separators** in relationship CSVs
4. **Document CSV structure** in your applications

### Error Handling

1. **Catch exceptions** at application level
2. **Log all errors** for debugging
3. **Provide user feedback** via ConsoleHelper
4. **Continue processing** after individual failures

## Extending the Library

### Adding New Processors

To add a new processor:

1. Create a class in the `Helpers` folder
2. Accept `IRestClient` and CSV path in constructor
3. Implement async processing methods
4. Use ConsoleHelper for user feedback
5. Return string status or custom result object
6. Add comprehensive error handling

Example:
```csharp
public class CustomProcessor
{
    private readonly IRestClient _client;
    private readonly string _csvFilePath;

    public CustomProcessor(IRestClient client, string csvFilePath)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _csvFilePath = csvFilePath ?? throw new ArgumentNullException(nameof(csvFilePath));
    }

    public async Task<string> ProcessAsync(string contentType, bool testMode = false)
    {
        // Implementation
    }
}
```

### Adding New CSV Models

Define CSV models with CsvHelper attributes:

```csharp
public class CustomMapping
{
    [Name("ColumnName1")]
    public string Property1 { get; set; } = string.Empty;

    [Name("ColumnName2")]
    public Guid Property2 { get; set; }
}
```

## Compatibility

- **.NET**: 9.0 or later
- **Sitefinity**: Version 15.4 or compatible
- **C#**: Language version 13.0

## Security Considerations

- **Never log access keys** or sensitive configuration
- **Validate all input** from CSV files
- **Use secure connections** (HTTPS) for Sitefinity API
- **Handle credentials** according to security best practices

## License

[MIT License - or specify your license]

## Author

Jonathan Read

## Contributing

Contributions welcome! Please:
1. Add unit tests for new functionality
2. Follow existing code style and patterns
3. Update documentation
4. Ensure all tests pass
