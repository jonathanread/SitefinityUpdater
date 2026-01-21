# Sitefinity Relationship Builder

A .NET 9 console application for batch building relationships between content items in Sitefinity CMS using CSV-based mapping.

## Overview

This tool connects to a Sitefinity CMS instance via the REST API and creates relationships between content items based on mappings defined in a CSV file. It's ideal for:
- Bulk relationship creation during content migration
- Establishing related content connections after data import
- Building cross-references between content types

## Features

- **CSV-Based Relationship Mapping**: Define parent-child relationships in an easy-to-edit CSV format
- **Multi-Item Relationships**: Support for one-to-many relationships (one parent with multiple related items)
- **Batch Processing**: Processes relationships in batches of 50 for optimal performance
- **Flexible ID Format**: Parse comma, semicolon, or pipe-separated related item IDs
- **Test Mode**: Process a single relationship first to verify the configuration
- **Rich Console Output**: Color-coded logging for easy monitoring
- **Concurrent Execution**: Uses async/await and Task.WhenAll for parallel processing within batches

## Prerequisites

- .NET 9.0 SDK or later
- Access to a Sitefinity CMS instance (version 15.4 or compatible)
- Sitefinity REST API access key
- Site ID of the Sitefinity site you want to update

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
  "RelationshipCsvFilePath": "relationships.csv"
}
```

**Configuration Parameters:**

- **Url**: The base URL of your Sitefinity REST API endpoint (typically ends with `/sf/system/`)
- **AccessKey**: Your Sitefinity REST API access key (base64 encoded)
- **SiteId**: The GUID of the site you want to connect to
- **RelationshipCsvFilePath**: (Optional) Filename or path to the CSV file. Defaults to `relationships.csv`

### CSV Relationship Mapping File

Create a CSV file to define relationships between content items:

```csv
ParentItemId,RelatedContentType,RelatedItemIds
f9c59b18-eaf3-4813-893b-307a1eddd46a,newsitems,a1b2c3d4-e5f6-7890-abcd-ef1234567890
e4d3c2b1-a9f8-7654-3210-fedcba987654,newsitems,b2c3d4e5-f6a7-8901-bcde-f23456789012;c3d4e5f6-a7b8-9012-cdef-345678901234
12345678-1234-1234-1234-123456789012,newsitems,23456789-2345-2345-2345-234567890123|34567890-3456-3456-3456-345678901234
```

**CSV Column Descriptions:**

- **ParentItemId**: The GUID of the parent content item that will have the relationship
- **RelatedContentType**: The content type of the related items (e.g., `newsitems`, `Telerik.Sitefinity.DynamicTypes.Model.News.NewsItems`)
- **RelatedItemIds**: One or more GUIDs of related items, separated by comma (,), semicolon (;), or pipe (|)

**Important Notes:**
- The CSV file should be placed in the application directory (or specify a custom path in `appsettings.json`)
- Multiple related items can be specified using comma, semicolon, or pipe separators
- All GUIDs must be valid - invalid GUIDs will be skipped with warnings
- The Sitefinity API will validate that items exist when creating relationships

## Usage

### Running the Application

```bash
dotnet run --project SitefinityContentUpdater.Relationships
```

Or, after building, run the executable directly:

```bash
cd SitefinityContentUpdater.Relationships/bin/Debug/net9.0
./SitefinityContentUpdater.Relationships.exe
```

### Interactive Workflow

1. **Configuration Loading**: The app reads connection details from `appsettings.json`
   - Displays the CSV file path being used

2. **Connection & Validation**: 
   - Connects to your Sitefinity site
   - Displays the site name for confirmation
   - Prompts for confirmation to proceed

3. **Parent Content Type**: Enter the parent content type
   - Examples: `newsitems`, `Telerik.Sitefinity.DynamicTypes.Model.News.NewsItems`
   - This is the content type that will have the relationship field

4. **Relationship Field Name**: Enter the relationship field name
   - Examples: `RelatedNews`, `RelatedProducts`, `Categories`
   - This must be an existing relationship field in the parent content type

5. **Test Mode Selection**: Choose processing mode
   - **Test Mode (y)**: Process only 1 relationship for validation
   - **Full Mode (n)**: Process all relationships from the CSV

6. **Confirmation** (Full Mode only): Final confirmation before processing all relationships

7. **Processing**: The app processes relationships with detailed logging
   - Loads relationship mappings from CSV
   - Parses related item IDs
   - Creates relationships in batches of 50 using concurrent API calls
   - Reports success and failures

### Example Session

```
Sitefinity Relationship Builder
Use this tool to build relationships between content items from a CSV file

Using site URL from config: https://localhost:44358/sf/system/
Using access key from config
Using site ID from config: 11a3d5f0-67c1-47cb-9435-4f6da07152b7
Relationship CSV file path: C:\app\SitefinityContentUpdater.Relationships\bin\Debug\net9.0\relationships.csv
Successfully connected to Sitefinity site: Default Site
Is this the correct site? y/n
y
Proceeding with the update...

Enter the parent content type (e.g. newsitems):
newsitems

Enter the relationship field name (e.g. RelatedNews):
RelatedNews

Do you want to test on 1 relationship first? (y/n)
y

Running in TEST MODE - Only 1 relationship will be processed.
Loaded 3 relationship mapping(s) from CSV.
TEST MODE: Processing only 1 relationship mapping.
Processing batch 1 of 1 (3 items)...
Batch 1 completed: 3 relationship(s) created.
Updated item f9c59b18-eaf3-4813-893b-307a1eddd46a: Added 3 related item(s) to 'RelatedNews'
TEST MODE COMPLETED: Processed 1 mapping(s), Updated 1, Failed 0.
Review the results above. Run again without test mode to process all mappings.

Process completed. Press any key to exit.
```

### Example with Large Batch

```
Loaded 5 relationship mapping(s) from CSV.
Processing 5 relationship mapping(s).
Processing batch 1 of 3 (50 items)...
Batch 1 completed: 50 relationship(s) created.
Processing batch 2 of 3 (50 items)...
Batch 2 completed: 50 relationship(s) created.
Processing batch 3 of 3 (25 items)...
Batch 3 completed: 25 relationship(s) created.
Updated item {guid}: Added 125 related item(s) to 'RelatedNews'
```

## How It Works

### Relationship Building Flow

1. **CSV Loading**: Reads the CSV file and parses relationship mappings
2. **ID Parsing**: Parses the comma/semicolon/pipe-separated related item IDs
3. **Batch Processing**: Divides related items into batches of 50
4. **Concurrent Execution**: Uses `Task.WhenAll` to create relationships concurrently within each batch
5. **API Validation**: Sitefinity's `RelateItem` API validates that items exist and creates relationships
6. **Result Reporting**: Logs success or failure for each relationship

### Batch Processing

- **Batch Size**: 50 items per batch
- **Concurrent Execution**: All items within a batch are processed concurrently
- **Sequential Batches**: Batches are processed sequentially to avoid overwhelming the API
- **Progress Logging**: Shows current batch progress and completion

### Separator Support

The tool supports multiple separators for related item IDs:
- **Comma (,)**: `id1,id2,id3`
- **Semicolon (;)**: `id1;id2;id3`
- **Pipe (|)**: `id1|id2|id3`

Choose the separator that works best for your workflow.

## Error Handling

The tool includes comprehensive error handling:

- **Invalid GUIDs**: Warns about invalid GUID formats in the CSV and skips them
- **API Errors**: Catches and logs errors from the Sitefinity API (e.g., item not found, permission issues)
- **Batch Failures**: If a batch fails, the error is logged and processing continues with the next batch
- **Graceful Degradation**: Invalid items are skipped, but valid items are still processed

## Best Practices

### CSV File Preparation

1. **Validate GUIDs**: Ensure all Parent and Related Item IDs are valid GUIDs
2. **Use Consistent Separators**: Stick to one separator type for consistency
3. **Test Small Batches**: Start with a few relationships to validate your configuration
4. **Organize Data**: Group related items logically in your CSV for easier maintenance

### Testing Strategy

1. **Always start with test mode** to validate one relationship
2. Review the console output:
   - Check batch processing logs
   - Look for any warnings or errors
   - Verify relationship counts
3. If test looks good, run in full mode
4. Monitor the batch processing progress

### Performance Optimization

- **Batch Size**: The default 50 items per batch is optimized for most scenarios
- **Concurrent Execution**: Up to 50 concurrent API calls per batch for maximum throughput
- **Large Datasets**: For thousands of relationships, the tool automatically divides them into manageable batches

## Troubleshooting

### CSV File Not Found
```
CSV file not found at: [path]
```
**Solution**: 
- Ensure the CSV file exists in the application directory
- Update `RelationshipCsvFilePath` in `appsettings.json` if using a different filename

### Invalid GUID Format
```
Invalid GUID format: [value]
```
**Solution**: 
- Check that all IDs in the CSV are valid GUIDs
- Remove any extra spaces or special characters
- Ensure no line breaks within ID fields

### Relationship Creation Failed
```
Failed to update item [guid]: [error message]
```
**Solution**: 
- Verify the relationship field name exists in the parent content type
- Check that parent and related items exist in Sitefinity
- Ensure the relationship field accepts the related content type
- Verify you have permissions to create relationships

### Batch Processing Errors
```
Error processing relationship for item [guid]: [error]
```
**Solution**:
- Check the specific error message for details
- Verify all item IDs in the batch are valid
- Ensure the Sitefinity instance is responding correctly

## Integration Scenarios

This tool is particularly useful for:

1. **Post-Migration Relationship Building**
   - After migrating content, rebuild relationships from mapping data
   - Connect related content across different content types

2. **Bulk Relationship Creation**
   - Establish relationships for hundreds or thousands of content items at once
   - Create cross-references based on external data sources

3. **Content Organization**
   - Group related news articles, blog posts, or products
   - Build category associations from CSV exports

4. **Data Integration**
   - Import relationship data from external systems
   - Maintain relationships during environment promotions

## Technical Details

### Architecture

- **RelationshipProcessor**: Core class that handles relationship creation
  - `BuildRelationshipsAsync`: Main entry point
  - `ProcessRelationshipAsync`: Processes a single relationship mapping
  - `BatchRelateItemsAsync`: Handles batch processing of RelateItem calls
  - `ParseRelatedItemIds`: Parses comma/semicolon/pipe-separated IDs
  - `LoadRelationshipsFromCsv`: Loads and parses the CSV file

### API Usage

The tool uses the Sitefinity REST SDK's `RelateItem` method:

```csharp
await _client.RelateItem(new RelateArgs()
{
    Type = contentType,
    Id = parentItemId.ToString(),
    RelationName = relationshipFieldName,
    RelatedItemId = relatedItemId.ToString()
});
```

## Dependencies

This project depends on the **SitefinityContentUpdater.Core** project which includes:

- **Progress.Sitefinity.RestSdk** (v15.4.8622.28): Sitefinity REST API client
- **CsvHelper** (v33.1.0): CSV file reading with attribute-based mapping
- **Microsoft.Extensions.Configuration** (v9.0.0): Configuration management

## Security Considerations

- **Never commit `appsettings.json` with real credentials to source control**
- Add `appsettings.json` to `.gitignore`
- Use environment-specific configuration files
- The access key should be treated as a secret

## Performance Characteristics

- **Batch Size**: 50 items per batch
- **Concurrency**: Up to 50 concurrent API calls per batch
- **Throughput**: Depends on network latency and Sitefinity server performance
- **Memory**: Minimal - processes one relationship mapping at a time

## Author

Jonathan Read

## License

[MIT License - or specify your license]
