# Sitefinity Content Updater

[![.NET Desktop CI](https://github.com/jonathanread/SitefinityUpdater/actions/workflows/dotnet-desktop.yml/badge.svg)](https://github.com/jonathanread/SitefinityUpdater/actions/workflows/dotnet-desktop.yml)

A .NET 9 solution for batch updating content and building relationships in Sitefinity CMS. Includes two console applications and a shared core library.

## Projects

### 1. SitefinityContentUpdater
Console application for batch updating rich text content fields with specialized support for migrating and updating image references using CSV-based ID mapping.

### 2. SitefinityContentUpdater.Relationships
Console application for batch building relationships between content items using CSV-based mapping.

### 3. ImportJson
Console application for importing JSON into Sitefinity content types, including optional nested child content creation via `Child_<ContentType>` fields.

- See full docs: [ImportJson README](ImportJson/README.md)

### 4. SitefinityContentUpdater.Core
Shared class library containing core functionality, helpers, and REST client utilities used by the console applications.

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

## ImportJson

A console application for importing JSON content to Sitefinity using the REST SDK.

### JSON Structure

The root JSON object should be:

```json
{
  "SitefinityContentTypeName": [
    {
      "Title": "Parent 1",
      "Description": "Any fields are sent as-is",
      "Child_Telerik.Sitefinity.DynamicTypes.Model.Modules.Products": [
        {
          "Title": "Child item A",
          "Price": 19.99
        },
        {
          "Title": "Child item B",
          "Price": 29.99
        }
      ]
    },
    {
      "Title": "Parent 2",
      "Child_newsitems": {
        "Title": "Single child object also supported"
      }
    }
  ]
}
```

### Notes

- Top-level key = Sitefinity content type name.
- Value must be an array of objects.
- Any field starting with `Child_` is treated as child content:
  - The part after `Child_` is the child Sitefinity content type name.
- Parent items are created first.
- Child items are created after and receive `ParentId` from the created parent item ID.
- All non-child fields are sent to Sitefinity exactly as provided.

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
  "SourceSite": {
    "Url": "https://source-sitefinity-site.com/sf/system/",
    "AccessKey": "source-access-key-here",
    "SiteId": "source-site-id-guid-here"
  },
  "TargetSite": {
    "Url": "https://target-sitefinity-site.com/sf/system/",
    "AccessKey": "target-access-key-here",
    "SiteId": "target-site-id-guid-here"
  }
}
```

### ImportJson - appsettings.json

```json
{
  "Sitefinity": {
    "Url": "https://your-sitefinity-site.com/sf/system/",
    "AccessKey": "your-access-key-here",
    "SiteId": "your-site-id-guid-here"
  },
  "JsonFilePath": "import.json"
}
```

**Configuration Parameters:**

- **Url**: The base URL of your Sitefinity REST API endpoint (typically ends with `/sf/system/`)
- **AccessKey**: Your Sitefinity REST API access key (base64 encoded)
- **SiteId**: The GUID of the site you want to connect to
- **CsvFilePath**: Path to the CSV file containing image ID mappings
- **JsonFilePath**: Path to the JSON file used by `ImportJson`

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

### Running ImportJson

```bash
dotnet run --project ImportJson -- "path/to/import.json"
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
??? ImportJson/                                  # JSON import console app
?   ??? Program.cs
?   ??? appsettings.json
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

See [SitefinityUpdater.Core.Tests/README.md](SitefinityUpdater.Core.Tests/README.md) for detailed test documentation.

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
