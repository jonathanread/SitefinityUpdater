# Sitefinity Content Updater

[![.NET Desktop CI](https://github.com/jonathanread/SitefinityUpdater/actions/workflows/dotnet-desktop.yml/badge.svg)](https://github.com/jonathanread/SitefinityUpdater/actions/workflows/dotnet-desktop.yml)

A .NET 9 solution for batch managing content in Sitefinity CMS via the REST API. The solution contains several focused console tools, a shared core library, and a full unit/integration test suite.

---

## Projects

### [SitefinityContentUpdater](SitefinityUpdater/)
Batch-updates rich text content fields across Sitefinity content items. Supports CSV-based image ID mapping so that image references can be remapped from a source environment to a target environment during migration.

### [SitefinityContentUpdater.Relationships](SitefinityContentUpdater.Relationships/README.md)
Builds relationships between existing Sitefinity content items in bulk. Reads parent/child ID pairs from a CSV file and calls the Sitefinity REST API to create each relationship, with support for one-to-many mappings and test mode.

### [ImportJson](ImportJson/README.md)
Imports structured JSON data into Sitefinity content types. Supports recursive child content creation (`Child_` prefix), related-item linking (`Related_` prefix), and taxonomy assignment (`Taxon_` prefix), making it suitable for complex content migrations.

### [SitefinityContentUpdate.PageScaffolding](SitefinityContentUpdate.PageScaffolding/)
Scaffolds Sitefinity page hierarchies from a structured definition. Creates pages, assigns templates, resolves parent pages, and adds a default H1 content block — useful for seeding site structure during a migration or initial build.

### [SitefinityUpdater.Core](SitefinityUpdater.Core/README.md)
Shared class library consumed by all console applications. Provides REST client management, batch content processing, relationship helpers, taxonomy resolution with caching, CSV-based data mapping, site validation, and console output utilities.

### [SitefinityUpdater.Core.Tests](SitefinityUpdater.Core.Tests/README.md)
xUnit test project with 159 unit and integration tests covering all helpers and extension methods in `SitefinityUpdater.Core`. Uses Moq and FluentAssertions.

---

## Getting Started

### Prerequisites

- .NET 9.0 SDK or later
- Access to a Sitefinity CMS instance (version 15.4 or compatible)
- Sitefinity REST API access key and Site ID

### Install & Build

```bash
git clone https://github.com/jonathanread/SitefinityUpdater.git
cd SitefinityUpdater
dotnet restore
dotnet build
```

### Run a Tool

```bash
# Batch update content fields
dotnet run --project SitefinityUpdater

# Build relationships from CSV
dotnet run --project SitefinityContentUpdater.Relationships

# Import JSON data
dotnet run --project ImportJson

# Scaffold pages
dotnet run --project SitefinityContentUpdate.PageScaffolding
```

Each tool reads connection settings from its own `appsettings.json`. If any required value is missing, the tool prompts for it at runtime. See the individual project READMEs linked above for configuration details and examples.

### Run Tests

```bash
dotnet test
```

---

## Dependencies

- **Progress.Sitefinity.RestSdk** — Sitefinity REST API client
- **AngleSharp** — HTML parsing
- **CsvHelper** — CSV reading
- **Microsoft.Extensions.Configuration** — configuration management

---

## Author

Jonathan Read
