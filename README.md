# Sitefinity Content Updater

[![.NET Desktop CI](https://github.com/jonathanread/SitefinityUpdater/actions/workflows/dotnet-desktop.yml/badge.svg)](https://github.com/jonathanread/SitefinityUpdater/actions/workflows/dotnet-desktop.yml)

A .NET 9 solution for batch updating content and building relationships in Sitefinity CMS. Includes two console applications and a shared core library.

## Projects

### 1. SitefinityContentUpdater
Console application for batch updating rich text content fields with specialized support for migrating and updating image references using CSV-based ID mapping.

### 2. SitefinityContentUpdater.Relationships
Console application for batch building relationships between content items using CSV-based mapping.

### 3. SitefinityContentUpdater.Core
Shared class library containing core functionality, helpers, and REST client utilities used by both console applications.

---

## SitefinityContentUpdater

A console application for batch updating rich text content fields in Sitefinity CMS, with specialized support for migrating and updating image references using CSV-based ID mapping.

### Features

- **Batch Content Processing**: Update multiple content items efficiently with configurable batch sizes (50 items per batch)
- **CSV-Based Image Mapping**: Map source image IDs to target image IDs for content migration scenarios
- **Smart Image Matching**: Multiple matching strategies:
  - Source-to-Target ID mapping via CSV (priority)
  - Image Title matching
  - Direct ID matching from src attribute
- **Test Mode**: Process a single item first to verify changes before running full batch updates
- **Rich Console Output**: Color-coded logging for easy monitoring of the update process
- **Async/Concurrent Processing**: Efficient batch updates using async/await and parallel operations
- **Flexible Configuration**: Store credentials in `appsettings.json` or enter interactively

### CSV Image Mapping File

Create a CSV file to map source images to target images for migration scenarios:

```csv
Image Title,Source Id,Target Id
My Image Title,f9c59b18-eaf3-4813-893b-307a1eddd46a,a1b2c3d4-e5f6-7890-abcd-ef1234567890
Another Image,e4d3c2b1-a9f8-7654-3210-fedcba987654,b2c3d4e5-f6a7-8901-bcde-f23456789012
Image Without Target,12345678-1234-1234-1234-123456789012,N/A
```

---

## SitefinityContentUpdater.Relationships

A console application for batch building relationships between content items in Sitefinity CMS using CSV-based mapping.

### Features

- **CSV-Based Relationship Mapping**: Define parent-child relationships in an easy-to-edit CSV format
- **Multi-Item Relationships**: Support for one-to-many relationships (one parent with multiple related items)
- **Batch Processing**: Processes relationships in batches of 50 for optimal performance
- **Flexible ID Format**: Parse comma, semicolon, or pipe-separated related item IDs
- **Test Mode**: Process a single relationship first to verify the configuration
- **Concurrent Execution**: Uses async/await and Task.WhenAll for parallel processing within batches

### CSV Relationship Mapping File

Create a CSV file to define relationships between content items:

```csv
ParentItemId,RelatedContentType,RelatedItemIds
f9c59b18-eaf3-4813-893b-307a1eddd46a,newsitems,a1b2c3d4-e5f6-7890-abcd-ef1234567890
e4d3c2b1-a9f8-7654-3210-fedcba987654,newsitems,b2c3d4e5-f6a7-8901-bcde-f23456789012;c3d4e5f6-a7b8-9012-cdef-345678901234
12345678-1234-1234-1234-123456789012,newsitems,23456789-2345-2345-2345-234567890123|34567890-3456-3456-3456-345678901234
```

---

## SitefinityContentUpdater.Core

Shared class library containing core functionality used by both console applications.

### Components

#### Helpers
- **ConfigurationHelper**: Configuration loading and CSV path management
- **ConsoleHelper**: Color-coded console output utilities
- **ContentProcessor**: Content processing and image mapping logic
- **RelationshipProcessor**: Relationship creation and batch processing
- **SiteValidator**: Sitefinity site connection validation

#### REST Client
- **RestClientFactory**: REST client initialization
- **SitefinityConfig**: Sitefinity configuration model

#### Extensions
- **RestClientExtensions**: Extension methods for REST client operations

---

## Prerequisites

- .NET 9.0 SDK or later
- Access to a Sitefinity CMS instance (version 15.4 or compatible)
- Sitefinity REST API access key
- Site ID of the Sitefinity site you want to update

## Installation

1. Clone the repository:
```bash
git clone https://github.com/jonathanread/SitefinityUpdater.git
cd SitefinityUpdater
```

2. Restore NuGet packages:
```bash
dotnet restore
```

3. Build the solution:
```bash
dotnet build
```

## Configuration

Both console applications use similar configuration in their respective `appsettings.json` files:

### SitefinityContentUpdater - appsettings.json

```json
{
  "Sitefinity": {
    "Url": "https://your-sitefinity-site.com/sf/system/",
    "AccessKey": "your-access-key-here",
    "SiteId": "your-site-id-guid-here"
  },
  "CsvFilePath": "image_mappings_20251214_073902.csv"
}
```

### SitefinityContentUpdater.Relationships - appsettings.json

```json
{
  "Sitefinity": {
    "Url": "https://your-sitefinity-site.com/sf/system/",
    "AccessKey": "your-access-key-here",
    "SiteId": "your-site-id-guid-here"
  },
  "RelationshipCsvFilePath": "relationships.csv"
}
```

**Configuration Parameters:**

- **Url**: The base URL of your Sitefinity REST API endpoint (typically ends with `/sf/system/`)
- **AccessKey**: Your Sitefinity REST API access key (base64 encoded)
- **SiteId**: The GUID of the site you want to connect to
- **CsvFilePath**: Path to the CSV file containing image ID mappings
- **RelationshipCsvFilePath**: Path to the CSV file containing relationship mappings

> **Note**: If Sitefinity credentials are missing from the configuration file, the applications will prompt you to enter them at runtime.

## Getting Your Sitefinity Credentials

### Access Key

1. Log in to your Sitefinity backend
2. Navigate to **Administration** ? **Settings** ? **Advanced** ? **WebServices**
3. Find or create an access key for REST API access
4. Copy the access key value (it will be base64 encoded)

### Site ID

1. In Sitefinity backend, go to **Administration** ? **Sites**
2. Select your site
3. The Site ID (GUID) will be visible in the site properties or URL

## Usage

### Running the Content Updater

```bash
dotnet run --project SitefinityUpdater
```

### Running the Relationship Builder

```bash
dotnet run --project SitefinityContentUpdater.Relationships
```

## Solution Structure

```
SitefinityUpdater/
??? SitefinityUpdater/                           # Content updater console app
?   ??? Program.cs
?   ??? appsettings.json
?   ??? image_mappings_*.csv
??? SitefinityContentUpdater.Relationships/      # Relationship builder console app
?   ??? Program.cs
?   ??? appsettings.json
?   ??? relationships.csv
??? SitefinityUpdater.Core/                      # Shared core library
?   ??? Helpers/
?   ?   ??? ConfigurationHelper.cs
?   ?   ??? ConsoleHelper.cs
?   ?   ??? ContentProcessor.cs
?   ?   ??? RelationshipProcessor.cs
?   ?   ??? SiteValidator.cs
?   ??? RestClient/
?   ?   ??? RestClientFactory.cs
?   ?   ??? SitefinityConfig.cs
?   ??? Extensions/
?       ??? RestClientExtensions.cs
??? SitefinityUpdater.Core.Tests/                # Unit and integration tests
    ??? Helpers/
    ??? Integration/
    ??? RestClient/
    ??? README.md
```

## Dependencies

- **AngleSharp** (v1.4.0): HTML parsing and DOM manipulation
- **CsvHelper** (v33.1.0): CSV file reading with attribute-based mapping
- **Progress.Sitefinity.RestSdk** (v15.4.8622.28): Sitefinity REST API client
- **Microsoft.Extensions.Configuration** (v9.0.0): Configuration management
- **Microsoft.Extensions.Configuration.Json** (v9.0.0): JSON configuration provider

### Test Dependencies

- **xUnit** (v2.9.2): Test framework
- **Moq** (v4.20.72): Mocking framework
- **FluentAssertions** (v6.12.2): Assertion library
- **coverlet.collector** (v6.0.2): Code coverage

## Console Output

Both applications provide colored console output for better readability:

- ?? **Green**: Success messages, completed operations
- ?? **Cyan**: Information messages, configuration values, progress updates
- ?? **Yellow**: Warning messages (skipped items, missing CSV, no images found)
- ?? **Red**: Errors, required input prompts

## Testing

The solution includes comprehensive unit and integration tests:

```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --verbosity normal

# Run specific test class
dotnet test --filter "FullyQualifiedName~RelationshipProcessorTests"
```

**Test Coverage:**
- ? 84 total tests
- Unit tests for all helpers and core components
- Integration tests for workflows
- CSV parsing and validation tests
- Console I/O management tests

See [SitefinityUpdater.Core.Tests/README.md](SitefinityUpdater.Core.Tests/README.md) for detailed test documentation.

## Best Practices

### Testing Strategy

1. **Always start with test mode** on a single item/relationship
2. Review the console output carefully for any warnings or errors
3. If test looks good, run in full mode
4. Monitor the batch update/processing progress

### CSV File Management

1. **Validate data**: Ensure all GUIDs are valid before processing
2. **Version control**: Keep CSV files in version control for traceability
3. **Backup**: Always backup CSV files before making changes
4. **Document**: Use meaningful names and titles in CSV files

### Safety

- Both tools only modify items/relationships explicitly defined in the CSV
- All operations are logged with detailed information
- Test mode allows validation before bulk updates
- CSV files are optional for the content updater (falls back to title matching)

### Performance

- Batch size of 50 items balances performance and memory
- Concurrent operations using `Task.WhenAll` for efficiency
- CSV loaded once per batch to avoid repeated file I/O
- Async operations throughout for better responsiveness

## Security Considerations

- **Never commit `appsettings.json` with real credentials to source control**
- Add `appsettings.json` to `.gitignore` (or use `appsettings.Development.json`)
- Use environment-specific configuration files for different environments
- Consider using Azure Key Vault or environment variables for production credentials
- The access key is base64 encoded but should still be treated as a secret

## Common Use Cases

### Content Migration
1. Export image mappings from source Sitefinity instance
2. Create CSV with source and target image IDs
3. Run **SitefinityContentUpdater** to update image references
4. Create relationship mappings CSV
5. Run **SitefinityContentUpdater.Relationships** to rebuild relationships

### Bulk Operations
1. Use **SitefinityContentUpdater** for bulk image reference updates
2. Use **SitefinityContentUpdater.Relationships** for bulk relationship creation
3. Test mode for validation before full processing

### Environment Promotion
1. Map development IDs to production equivalents in CSV files
2. Run tools on target environment to update references and relationships

## Troubleshooting

### Connection Issues
```
Connection failed or site validation failed
```
**Solution**: 
- Verify URL is correct and ends with `/sf/system/`
- Ensure access key is valid and has appropriate permissions
- Check that Sitefinity REST API is enabled
- Verify Site ID is correct

### CSV Issues
```
CSV file not found at: [path]
```
**Solution**: 
- Ensure the CSV file exists in the application directory
- Update CSV path in `appsettings.json`
- Check file name and extension

### Invalid Data
```
Invalid GUID format: [value]
```
**Solution**: 
- Validate all GUIDs in CSV files
- Remove extra spaces or special characters
- Use proper CSV escaping for special characters

## Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.

## License

[MIT License - or specify your license]

## Author

Jonathan Read

## Support

For issues related to:
- **This tool**: Create an issue at https://github.com/jonathanread/SitefinityUpdater
- **Sitefinity REST API**: Consult Progress Sitefinity documentation
- **Sitefinity CMS**: Contact Progress Software support
