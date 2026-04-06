# ImportJson

`ImportJson` is a .NET 9 console app that imports JSON content into Sitefinity using `SitefinityContentUpdater.Core`.

## What it supports

- Root JSON format: `{ "SitefinityContentTypeName": [ { ... } ] }`
- Dynamic fields: every non-child field is sent as-is to Sitefinity REST create payload.
- Child content support using `Child_` prefix:
  - `Child_<ContentTypeName>`
  - Value can be an object or array of objects.
- Two-phase import:
  1. Parse full structure and detect children
  2. Create all top-level items first
  3. Create child items and set `ParentId` from created parent item ID

## Sample JSON

```json
{
  "Telerik.Sitefinity.DynamicTypes.Model.Products.Product": [
    {
      "Title": "Parent Product A",
      "Sku": "SKU-001",
      "Price": 99.95,
      "Child_Telerik.Sitefinity.DynamicTypes.Model.Products.ProductVariant": [
        {
          "Title": "Variant A1",
          "Code": "VAR-A1",
          "InStock": true
        },
        {
          "Title": "Variant A2",
          "Code": "VAR-A2",
          "InStock": false
        }
      ]
    },
    {
      "Title": "Parent Product B",
      "Sku": "SKU-002",
      "Child_newsitems": {
        "Title": "Related News Item"
      }
    }
  ]
}
```

## Configuration

Use `ImportJson/appsettings.json`:

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

## Running

From solution root:

```bash
dotnet run --project ImportJson -- "path/to/import.json"
```

If no argument is passed, the app uses `JsonFilePath` from config. If that is also missing, it prompts for a path.

## Notes

- Child content type is taken from the text after `Child_`.
- Child items receive `ParentId` automatically.
- Test mode can process only one top-level item before full run.
