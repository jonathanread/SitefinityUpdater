# .NET Version Upgrade Plan

## Overview

**Target**: Upgrade all projects from net9.0 → net10.0
**Scope**: 6 projects, 2-tier dependency graph, all SDK-style modern .NET

### Selected Strategy
**All-At-Once** — All projects upgraded simultaneously in a single operation.
**Rationale**: 6 projects, all on net9.0/net10.0, 2-tier shallow dependency graph — mechanical TFM bump with package updates.

---

## Tasks

### 01-prerequisites: Verify SDK and toolchain for net10.0

Confirm the .NET 10 SDK is installed and that any `global.json` in the repository is compatible with net10.0. This is a fast, non-destructive check that gates all subsequent work. If `global.json` pins an older SDK version, it must be updated to allow the net10.0 SDK before any project files are changed.

**Done when**: `dotnet --version` returns a .NET 10 SDK; any `global.json` allows net10.0 SDK; solution restores without SDK version errors.

---

### 02-upgrade-all-projects: Upgrade all projects to net10.0

Update the `TargetFramework` from `net9.0` to `net10.0` across all five projects that currently target net9.0 (`ImportJson`, `SitefinityUpdater.Core`, `SitefinityUpdater.Core.Tests`, `SitefinityUpdater`, `SitefinityUpdater.Relationships`). `SitefinityContentUpdate.PageScaffolding` already targets net10.0 and needs no TFM change.

Update the two packages with available upgrades: `Microsoft.Extensions.Configuration` and `Microsoft.Extensions.Configuration.Json` from 9.0.0 → 10.0.10. Replace the deprecated `xunit` package in `SitefinityContentUpdater.Core.Tests` with its supported replacement. Three projects (`ImportJson`, `SitefinityUpdater.Core`, `SitefinityContentUpdater.Core.Tests`) have behavioral API change flags (ruleId=Api.0003) — query the assessment for detail on affected files and resolve any resulting compilation errors.

After updating all project files and packages, restore dependencies and do a single solution-wide build pass, fixing all compilation errors before completing the task.

**Done when**: All 6 projects target net10.0; solution builds with 0 errors and 0 warnings; `Microsoft.Extensions.Configuration*` packages are on 10.0.10; deprecated xunit replaced; all tests pass.

---

### 03-final-validation: Final solution validation

Run the full test suite and confirm all tests pass on net10.0. Document any deferred items (e.g., post-migration CPM adoption) in a brief summary.

**Done when**: All tests pass; no build errors or warnings; execution log updated with final status.
