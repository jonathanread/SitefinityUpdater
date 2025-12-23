# Sitefinity Content Updater

A .NET 9 console application for batch updating rich text content fields in Sitefinity CMS, with specialized support for migrating and updating image references using CSV-based ID mapping.

## Overview

This tool connects to a Sitefinity CMS instance via the REST API and processes content items to update image `src` attributes in HTML content. It supports advanced image mapping scenarios using CSV files to map source image IDs to target image IDs, making it ideal for content migration between Sitefinity instances.

## Features

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

3. Build the project:
```bash
dotnet build
```

## Configuration

### appsettings.json

Configure your Sitefinity connection and CSV file path in `appsettings.json`:

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

**Configuration Parameters:**

- **Url**: The base URL of your Sitefinity REST API endpoint (typically ends with `/sf/system/`)
- **AccessKey**: Your Sitefinity REST API access key (base64 encoded)
- **SiteId**: The GUID of the site you want to connect to
- **CsvFilePath**: (Optional) Filename or path to the CSV file containing image ID mappings. Defaults to `image_mappings_20251214_073902.csv`

> **Note**: If Sitefinity credentials are missing from the configuration file, the application will prompt you to enter them at runtime.

### CSV Image Mapping File

Create a CSV file to map source images to target images for migration scenarios:

```csv
Image Title,Source Id,Target Id
My Image Title,f9c59b18-eaf3-4813-893b-307a1eddd46a,a1b2c3d4-e5f6-7890-abcd-ef1234567890
Another Image,e4d3c2b1-a9f8-7654-3210-fedcba987654,b2c3d4e5-f6a7-8901-bcde-f23456789012
Image Without Target,12345678-1234-1234-1234-123456789012,N/A
```

**CSV Column Descriptions:**

- **Image Title**: The title of the image (for reference/documentation purposes)
- **Source Id**: The GUID of the source image (extracted from HTML content)
- **Target Id**: The GUID of the target image in the destination Sitefinity instance, or "N/A" if no mapping exists

**Important Notes:**
- The CSV file should be placed in the application directory (or specify a custom path in `appsettings.json`)
- The file will be copied to the output directory during build
- Use "N/A" for images that don't have a target mapping (they will fall back to title-based matching)
- The CSV is loaded once per batch for optimal performance

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

### Running the Application

```bash
dotnet run --project SitefinityUpdater
```

Or, after building, run the executable directly:

```bash
cd SitefinityUpdater/bin/Debug/net9.0
./SitefinityUpdater.exe
```

### Interactive Workflow

1. **Configuration Loading**: The app reads connection details from `appsettings.json`
   - Displays the CSV file path being used
   - Loads image mappings from CSV if file exists

2. **Connection & Validation**: 
   - Connects to your Sitefinity site
   - Displays the site name for confirmation
   - Prompts for confirmation to proceed

3. **Content Type Selection**: Enter the content type to update
   - Examples: `newsitem`, `Telerik.Sitefinity.DynamicTypes.Model.News.NewsItem`
   - Can be any Sitefinity content type or custom dynamic module

4. **Field Name Selection**: Enter the rich text field name to update
   - Examples: `Content`, `Description`, or any custom field

5. **Test Mode Selection**: Choose processing mode
   - **Test Mode (y)**: Process only 1 item for validation
   - **Full Mode (n)**: Process all items in batches of 50

6. **Confirmation** (Full Mode only): Final confirmation before processing all items

7. **Processing**: The app processes items with detailed logging
   - Extracts image IDs from HTML src attributes
   - Maps source IDs to target IDs using CSV
   - Fetches image metadata from Sitefinity
   - Updates src attributes with new URLs
   - Removes sfref attributes
   - Batch updates modified items

### Example Session

```
Use this tool to update a content type rich text field
Using site URL from config: https://localhost:44358/sf/system/
Using access key from config
Using site ID from config: 11a3d5f0-67c1-47cb-9435-4f6da07152b7
CSV file path configured as: C:\app\SitefinityUpdater\bin\Debug\net9.0\image_mappings_20251214_073902.csv
Successfully connected to Sitefinity site: Default Site
Is this the correct site? y/n
y
Proceeding with the update...

Enter the content type you want to update (e.g. newsitem):
newsitem

Enter the field name to update (e.g. Content):
Content

Do you want to test on 1 item first? (y/n)
y

Running in TEST MODE - Only 1 item will be processed.
TEST MODE: Found 245 total items. Processing only 1 item.
Loaded 150 image mappings from CSV.
Item 12345678-abcd-efgh-ijkl-123456789012: Found 3 image(s).
Found 3 images from 2 title(s) and 2 target ID(s).
  Mapped source ID f9c59b18-eaf3-4813-893b-307a1eddd46a to target ID a1b2c3d4-e5f6-7890-abcd-ef1234567890
Item 12345678-abcd-efgh-ijkl-123456789012: Updated 3 image(s). Marked for update.
Batch update completed. Updated 1 items.
TEST MODE COMPLETED: Processed 1 item(s), Updated 1 item(s).
Review the results above. Run again without test mode to process all items.
```

## How It Works

### Image Processing Flow

1. **Content Retrieval**: Fetches content items in batches of 50 (or 1 in test mode)
2. **HTML Parsing**: Uses AngleSharp to parse HTML and extract `<img>` tags
3. **Image ID Extraction**: Uses regex to extract GUIDs from src attributes (pattern: `Item with ID: '[guid]'`)
4. **CSV Mapping Lookup**: 
   - Loads CSV mappings once per batch
   - Maps source image IDs to target image IDs
5. **Image Metadata Retrieval**: Fetches images from Sitefinity using:
   - Target IDs from CSV mappings
   - Image titles from HTML attributes
   - Combined filter query (Title in (...) or Id in (...))
6. **Smart Image Matching**: For each `<img>` tag:
   - **Priority 1**: Match source ID to target ID via CSV, then find image by target ID
   - **Priority 2**: Match by image title (if only one image in batch)
   - **Priority 3**: Match by title or direct ID comparison
7. **Content Update**: 
   - Updates `src` attribute with image's `Url` property
   - Removes `sfref` attribute
   - Saves modified HTML back to content item
8. **Batch Save**: Updates all modified items using `UpdateArgs` with content type

### Supported Image Matching Strategies

The tool uses a three-tier matching strategy:

1. **CSV ID Mapping** (Highest Priority)
   - Extracts source image ID from HTML src attribute
   - Looks up target ID in CSV mappings
   - Matches image by target ID
   - Ideal for migration between Sitefinity instances
   - Logs each successful mapping

2. **Title Matching**
   - Matches by image title or alt text
   - Useful when only one image is found
   - Good for images with unique, consistent titles

3. **Direct ID/Title Matching** (Fallback)
   - Matches by exact title comparison
   - Or matches by direct ID in src attribute
   - Used when CSV mapping not found or available

## Project Structure

```
SitefinityUpdater/
??? Helpers/
?   ??? ConfigurationHelper.cs    # Configuration loading and CSV path management
?   ??? ConsoleHelper.cs          # Color-coded console output utilities
?   ??? ContentProcessor.cs       # Main content processing and image mapping logic
?   ??? SiteValidator.cs          # Sitefinity site connection validation
??? RestClient/
?   ??? RestClientFactory.cs      # REST client initialization
?   ??? SitefinityConfig.cs       # Sitefinity configuration model
??? Program.cs                     # Application entry point and workflow
??? appsettings.json              # Configuration file (credentials, CSV path)
??? SitefinityUpdater.csproj      # Project file with dependencies
??? image_mappings_*.csv          # Image mapping data (user-provided)
```

## Dependencies

- **AngleSharp** (v1.4.0): HTML parsing and DOM manipulation
- **CsvHelper** (v33.1.0): CSV file reading with attribute-based mapping
- **Progress.Sitefinity.RestSdk** (v15.4.8622.28): Sitefinity REST API client
- **Microsoft.Extensions.Configuration** (v9.0.0): Configuration management
- **Microsoft.Extensions.Configuration.Json** (v9.0.0): JSON configuration provider

## Console Output

The application provides colored console output for better readability:

- ? **Green**: Success messages, completed operations
- ? **Cyan**: Information messages, configuration values, progress updates
- ? **Yellow**: Warning messages (skipped items, missing CSV, no images found)
- ? **Red**: Errors, required input prompts

## Troubleshooting

### CSV File Not Found
```
CSV file not found at: [path]. Proceeding without ID mapping.
```
**Solution**: 
- Ensure the CSV file exists in the application directory
- Update `CsvFilePath` in `appsettings.json` if using a different filename
- The tool will still work using title-based matching if CSV is missing

### CSV Mapping Error
```
Error loading CSV file: No members are mapped for type...
```
**Solution**: 
- Verify CSV has headers: `Image Title`, `Source Id`, `Target Id`
- Check that Source Id values are valid GUIDs
- Use "N/A" for Target Id when no mapping exists

### No Images Found
```
Item [id]: No images found in field 'Content'.
```
**Solution**: 
- Verify the field contains HTML with `<img>` tags
- Check that you specified the correct field name
- Field names are case-sensitive

### Update Errors
```
Error during batch update: The corresponding Sitefinity type cannot be inferred...
```
**Solution**: 
- This error has been fixed in the current version
- The tool now uses `UpdateArgs` with explicit content type
- Ensure you're using the correct content type name

### Images Not Matched Correctly
**Troubleshooting steps:**
1. Run in test mode to see detailed matching logs
2. Verify CSV mappings have correct source and target IDs
3. Check that image titles match exactly (case-sensitive)
4. Ensure source IDs in HTML match Source Id column in CSV
5. Look for "Mapped source ID..." messages to confirm CSV matching

### Connection Issues
```
Connection failed or site validation failed
```
**Solution**: 
- Verify URL is correct and ends with `/sf/system/`
- Ensure access key is valid and has appropriate permissions
- Check that Sitefinity REST API is enabled
- Verify Site ID is correct

## Best Practices

### Testing Strategy

1. **Always start with test mode** on a single item
2. Review the console output carefully:
   - Check "Mapped source ID..." messages for CSV mappings
   - Verify image counts and update counts
   - Look for any warnings or errors
3. If test looks good, run in full mode
4. Monitor the batch update progress

### CSV Mapping Best Practices

1. **Document your mappings**: Use meaningful Image Title values
2. **Handle missing mappings**: Use "N/A" for images without targets
3. **Validate GUIDs**: Ensure Source Id and Target Id are valid GUIDs
4. **Keep CSV updated**: Maintain the CSV as you discover new image mappings
5. **Back up your CSV**: Version control the CSV file along with code

### Safety

- The tool only updates items that contain `<img>` tags
- Items without images are skipped and logged
- All operations are logged with detailed information
- Test mode allows validation before bulk updates
- CSV file is optional - tool works without it

### Performance

- Batch size of 50 items balances performance and memory
- Concurrent updates using `Task.WhenAll` for efficiency
- CSV loaded once per batch to avoid repeated file I/O
- Async operations throughout for better responsiveness

## Security Considerations

- **Never commit `appsettings.json` with real credentials to source control**
- Add `appsettings.json` to `.gitignore` (or use `appsettings.Development.json`)
- Use environment-specific configuration files for different environments
- Consider using Azure Key Vault or environment variables for production credentials
- The access key is base64 encoded but should still be treated as a secret

## Migration Scenarios

This tool is particularly useful for:

1. **Content Migration Between Sitefinity Instances**
   - Export image mappings from source instance
   - Create CSV with source and target IDs
   - Run tool on target instance to update references

2. **Image Library Reorganization**
   - Map old image IDs to new reorganized image IDs
   - Bulk update content to reference new structure

3. **Environment Promotions**
   - Map development image IDs to production equivalents
   - Update content during environment promotion

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
