# ImportJson

`ImportJson` is a .NET 9 console app that imports JSON content into Sitefinity using `SitefinityContentUpdater.Core`.

## What it supports

- Root JSON format: `{ "SitefinityContentTypeName": [ { ... } ] }`
- Dynamic fields: every non-child field is sent as-is to Sitefinity REST create payload.
- Child content using `Child_<ContentTypeName>` — value can be an object or an array of objects.
- Nested (multi-level) children — a child item can itself contain further `Child_` entries.
- Multiple sibling child types on the same parent item.
- Pre-assigned parent via a `ParentId` field on top-level items.
- Related content using `Related_<RelationFieldName>` — creates standalone items and wires them to the parent via Sitefinity's `RelateItem` API.
  - Value must be an object with `ContentType` (string) and `Items` (array or single object).
  - Related items do **not** receive `ParentId`; they are linked through named relationship fields.
  - Related items can themselves carry `Child_` and `Related_` entries — nesting is fully recursive.
- Taxonomy fields using `Taxon_<FieldName>` — resolves taxon titles to GUIDs within a named taxonomy, creating missing taxa on demand. The resolved GUIDs are set on the item field before creation.
  - Value must be an object with `TaxonomyName` (string) and `Taxa` (array of title strings, or a single string).
  - Taxon resolution is cached per taxonomy for the entire import run — each unique title is only created once.
  - Supported on top-level items, child items, and related items.
- Import order:
  1. Parse the full structure and detect all child, related, and taxonomy relationships.
  2. Create all top-level items first (taxa resolved inline before each create).
  3. Recursively create child items, injecting `ParentId` from the created ancestor's ID.
  4. Create related items, then call `RelateItem` to link them to their parent.
  5. Recurse into child/related collections on each newly created related item.

---

## Relationship samples

### 1 — Flat import (no children)

Plain dynamic content items with no nested relationships. Every field maps directly to a Sitefinity REST field.

```json
{
  "Telerik.Sitefinity.DynamicTypes.Model.Catalogue.Item": [
    {
      "Title": "Item One",
      "UrlName": "item-one",
      "Summary": "A short description of the item.",
      "Content": "<p>Full HTML content here.</p>"
    },
    {
      "Title": "Item Two",
      "UrlName": "item-two",
      "Summary": "Another item with no child relationships."
    }
  ]
}
```

---

### 2 — Parent with single-level children

Top-level items own one or more child collections. Children are created after the parent and automatically receive `ParentId`.

```json
{
  "Telerik.Sitefinity.DynamicTypes.Model.Library.Book": [
    {
      "Title": "Book A",
      "Isbn": "ISBN-001",
      "Child_Telerik.Sitefinity.DynamicTypes.Model.Library.Chapter": [
        { "Title": "Chapter 1", "UrlName": "chapter-1", "SortOrder": 1 },
        { "Title": "Chapter 2", "UrlName": "chapter-2", "SortOrder": 2 }
      ]
    },
    {
      "Title": "Book B",
      "Isbn": "ISBN-002",
      "Child_Telerik.Sitefinity.DynamicTypes.Model.Library.Chapter": {
        "Title": "Introduction",
        "UrlName": "introduction",
        "SortOrder": 1
      }
    }
  ]
}
```

> `Child_` value can be a single object or an array of objects.

---

### 3 — Pre-assigned parent (`ParentId`) with nested two-level children

When the top-level item already belongs to an existing parent in Sitefinity, supply `ParentId` directly on the item. Children can themselves contain further `Child_` entries, creating a multi-level hierarchy.

```json
{
  "Telerik.Sitefinity.DynamicTypes.Model.Store.Product": [
    {
      "Title": "Product A",
      "ParentId": "00000000-0000-0000-0000-000000000001",
      "Sku": "PROD-A",
      "UrlName": "product-a",
      "Child_Telerik.Sitefinity.DynamicTypes.Model.Store.OptionGroup": [
        {
          "Title": "Size",
          "UrlName": "size",
          "Child_Telerik.Sitefinity.DynamicTypes.Model.Store.Option": [
            { "Title": "Small",  "UrlName": "small",  "Value": "S" },
            { "Title": "Medium", "UrlName": "medium", "Value": "M" },
            { "Title": "Large",  "UrlName": "large",  "Value": "L" }
          ]
        },
        {
          "Title": "Color",
          "UrlName": "color",
          "Child_Telerik.Sitefinity.DynamicTypes.Model.Store.Option": [
            { "Title": "Black", "UrlName": "black", "Value": "BLK" },
            { "Title": "White", "UrlName": "white", "Value": "WHT" }
          ]
        }
      ]
    }
  ]
}
```

---

### 4 — Pre-assigned parent with nested children **and** multiple sibling child types

A single item can carry several independent `Child_` collections at the same level. Here `OptionGroup` (with its own nested `Option` children) and `Tag` are both direct children of the same parent item.

```json
{
  "Telerik.Sitefinity.DynamicTypes.Model.Store.Product": [
    {
      "Title": "Product B",
      "ParentId": "00000000-0000-0000-0000-000000000001",
      "Sku": "PROD-B",
      "UrlName": "product-b",
      "Child_Telerik.Sitefinity.DynamicTypes.Model.Store.OptionGroup": [
        {
          "Title": "Size",
          "UrlName": "size",
          "Child_Telerik.Sitefinity.DynamicTypes.Model.Store.Option": [
            { "Title": "Small",  "UrlName": "small",  "Value": "S" },
            { "Title": "Medium", "UrlName": "medium", "Value": "M" }
          ]
        },
        {
          "Title": "Color",
          "UrlName": "color",
          "Child_Telerik.Sitefinity.DynamicTypes.Model.Store.Option": [
            { "Title": "Black", "UrlName": "black", "Value": "BLK" },
            { "Title": "White", "UrlName": "white", "Value": "WHT" }
          ]
        }
      ],
      "Child_Telerik.Sitefinity.DynamicTypes.Model.Store.Tag": [
        { "Title": "New Arrival", "UrlName": "new-arrival" },
        { "Title": "Sale",        "UrlName": "sale" }
      ]
    }
  ]
}
```

---

### 5 — Pre-assigned parent with nested children (no sibling child types)

Same two-level nesting as above but without an additional sibling `Child_` collection. Useful when only one grouped child structure is needed.

```json
{
  "Telerik.Sitefinity.DynamicTypes.Model.Store.Product": [
    {
      "Title": "Product C",
      "ParentId": "00000000-0000-0000-0000-000000000002",
      "Sku": "PROD-C",
      "UrlName": "product-c",
      "Description": "A product with grouped options but no flat tag list.",
      "Child_Telerik.Sitefinity.DynamicTypes.Model.Store.OptionGroup": [
        {
          "Title": "Size",
          "UrlName": "size",
          "Child_Telerik.Sitefinity.DynamicTypes.Model.Store.Option": [
            { "Title": "XS", "UrlName": "xs", "Value": "XS" },
            { "Title": "S",  "UrlName": "s",  "Value": "S" },
            { "Title": "M",  "UrlName": "m",  "Value": "M" }
          ]
        },
        {
          "Title": "Color",
          "UrlName": "color",
          "Child_Telerik.Sitefinity.DynamicTypes.Model.Store.Option": [
            { "Title": "Red",  "UrlName": "red",  "Value": "RED" },
            { "Title": "Blue", "UrlName": "blue", "Value": "BLU" }
          ]
        }
      ]
    }
  ]
}
```

---

## Related_ samples

The `Related_<FieldName>` prefix creates standalone items and links them to the parent through a named Sitefinity relationship field. The value must be an object with two required properties:

| Property | Description |
|---|---|
| `ContentType` | Fully qualified or short Sitefinity content type name of the items to create |
| `Items` | Array of item objects (or a single object) to create and relate |

---

### 6 — Simple related items

A top-level item relates to one or more items of a different content type through a named field.

```json
{
  "Telerik.Sitefinity.DynamicTypes.Model.Articles.Article": [
    {
      "Title": "Article One",
      "UrlName": "article-one",
      "Related_RelatedAuthors": {
        "ContentType": "Telerik.Sitefinity.DynamicTypes.Model.Authors.Author",
        "Items": [
          { "Title": "Jane Smith", "UrlName": "jane-smith", "Bio": "Senior editor." },
          { "Title": "John Doe",   "UrlName": "john-doe",   "Bio": "Staff writer." }
        ]
      }
    }
  ]
}
```

---

### 7 — Multiple sibling related fields

A single item can carry several independent `Related_` collections, each linking to a different relationship field (and optionally a different content type).

```json
{
  "Telerik.Sitefinity.DynamicTypes.Model.Articles.Article": [
    {
      "Title": "Article Two",
      "UrlName": "article-two",
      "Related_RelatedAuthors": {
        "ContentType": "Telerik.Sitefinity.DynamicTypes.Model.Authors.Author",
        "Items": [
          { "Title": "Jane Smith", "UrlName": "jane-smith" }
        ]
      },
      "Related_RelatedTopics": {
        "ContentType": "Telerik.Sitefinity.DynamicTypes.Model.Topics.Topic",
        "Items": [
          { "Title": "Technology", "UrlName": "technology" },
          { "Title": "Design",     "UrlName": "design" }
        ]
      }
    }
  ]
}
```

---

### 8 — Related items that themselves have related items (recursive)

Related items can carry their own `Related_` entries. The importer recurses as deep as needed — create the item, relate it to its parent, then process its own relationships.

```json
{
  "Telerik.Sitefinity.DynamicTypes.Model.Store.Product": [
    {
      "Title": "Product A",
      "UrlName": "product-a",
      "Related_RelatedCategories": {
        "ContentType": "Telerik.Sitefinity.DynamicTypes.Model.Store.Category",
        "Items": [
          {
            "Title": "Outerwear",
            "UrlName": "outerwear",
            "Related_RelatedDepartments": {
              "ContentType": "Telerik.Sitefinity.DynamicTypes.Model.Store.Department",
              "Items": [
                {
                  "Title": "Menswear",
                  "UrlName": "menswear",
                  "Related_RelatedFloors": {
                    "ContentType": "Telerik.Sitefinity.DynamicTypes.Model.Store.Floor",
                    "Items": [
                      { "Title": "Level 2", "UrlName": "level-2" }
                    ]
                  }
                }
              ]
            }
          }
        ]
      }
    }
  ]
}
```

---

### 9 — Mixed Child_ and Related_ on the same item

`Child_` and `Related_` can coexist. Children receive `ParentId`; related items are linked via the relationship field. Both support further nesting inside them.

```json
{
  "Telerik.Sitefinity.DynamicTypes.Model.Store.Product": [
    {
      "Title": "Product B",
      "UrlName": "product-b",
      "Child_Telerik.Sitefinity.DynamicTypes.Model.Store.OptionGroup": [
        {
          "Title": "Size",
          "UrlName": "size",
          "Child_Telerik.Sitefinity.DynamicTypes.Model.Store.Option": [
            { "Title": "S", "UrlName": "s", "Value": "S" },
            { "Title": "M", "UrlName": "m", "Value": "M" }
          ]
        }
      ],
      "Related_RelatedCategories": {
        "ContentType": "Telerik.Sitefinity.DynamicTypes.Model.Store.Category",
        "Items": [
          { "Title": "Basics", "UrlName": "basics" }
        ]
      }
    }
  ]
}
```

---

## Taxon_ samples

The `Taxon_<FieldName>` prefix resolves (or creates) taxon titles inside a named Sitefinity taxonomy and sets the resulting GUIDs on the item field before it is created.

The value must be an object with two required properties:

| Property | Description |
|---|---|
| `TaxonomyName` | The Sitefinity taxonomy name to resolve taxa within (e.g. `"Tags"`, `"Categories"`) |
| `Taxa` | Array of taxon title strings to resolve, or a single string |

---

### 10 — Single taxonomy field

Assign one or more tags to an item. Existing taxa are resolved by title; missing ones are created automatically.

```json
{
  "Telerik.Sitefinity.DynamicTypes.Model.Articles.Article": [
    {
      "Title": "Article One",
      "UrlName": "article-one",
      "Taxon_Tags": {
        "TaxonomyName": "Tags",
        "Taxa": ["Technology", "Design", "Open Source"]
      }
    }
  ]
}
```

---

### 11 — Multiple taxonomy fields on the same item

An item can carry any number of independent `Taxon_` fields, each targeting a different taxonomy and Sitefinity field.

```json
{
  "Telerik.Sitefinity.DynamicTypes.Model.Articles.Article": [
    {
      "Title": "Article Two",
      "UrlName": "article-two",
      "Taxon_Tags": {
        "TaxonomyName": "Tags",
        "Taxa": ["Technology", "Design"]
      },
      "Taxon_Category": {
        "TaxonomyName": "Categories",
        "Taxa": ["News"]
      }
    }
  ]
}
```

---

### 12 — Single taxon shorthand

When only one taxon is needed, `Taxa` can be a plain string instead of an array.

```json
{
  "Telerik.Sitefinity.DynamicTypes.Model.Articles.Article": [
    {
      "Title": "Article Three",
      "UrlName": "article-three",
      "Taxon_Tags": {
        "TaxonomyName": "Tags",
        "Taxa": "Technology"
      }
    }
  ]
}
```

---

### 13 — Taxonomy fields on child items

`Taxon_` fields work anywhere `Child_` and `Related_` entries are supported. Taxa are resolved before each item is created at any nesting depth.

```json
{
  "Telerik.Sitefinity.DynamicTypes.Model.Store.Product": [
    {
      "Title": "Product A",
      "UrlName": "product-a",
      "Taxon_Tags": {
        "TaxonomyName": "Tags",
        "Taxa": ["Sale", "New Arrival"]
      },
      "Child_Telerik.Sitefinity.DynamicTypes.Model.Store.Variant": [
        {
          "Title": "Variant S",
          "UrlName": "variant-s",
          "Taxon_Tags": {
            "TaxonomyName": "Tags",
            "Taxa": ["Sale"]
          }
        }
      ]
    }
  ]
}
```

> The taxon `"Sale"` is only created **once** regardless of how many items reference it. The `TaxonomyProcessor` caches all resolved and created taxa for the duration of the import run.

---

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

- The child content type name is taken from the text after `Child_` — this can be a short name (e.g. `newsitems`) or a fully qualified type name.
- Child items receive `ParentId` automatically from the created ancestor.
- `ParentId` on a top-level item assigns it to an existing parent already present in Sitefinity.
- `Related_<FieldName>` links items through Sitefinity's relationship API rather than `ParentId`.
- Related items are standalone — they are created independently and then associated via the named field.
- Both `Child_` and `Related_` nesting can go arbitrarily deep; each level is fully resolved before the next is processed.
- `Taxon_<FieldName>` resolves taxon titles to GUIDs before item creation. Missing taxa are created automatically.
- Taxa are resolved from the named taxonomy via `TaxonomyName`; the field is set as a `Guid[]` on the item.
- Taxon resolution is cached per taxonomy for the full import run — the same title is never created twice.
- `Child_`, `Related_`, and `Taxon_` can all coexist on the same item and at any nesting depth.
- Test mode can process only one top-level item before a full run.
- Test mode can process only one top-level item before a full run.
