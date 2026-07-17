# 02-upgrade-all-projects: Upgrade all projects to net10.0

Update the `TargetFramework` from `net9.0` to `net10.0` across all five projects that currently target net9.0 (`ImportJson`, `SitefinityUpdater.Core`, `SitefinityUpdater.Core.Tests`, `SitefinityUpdater`, `SitefinityUpdater.Relationships`). `SitefinityContentUpdate.PageScaffolding` already targets net10.0 and needs no TFM change.

Update the two packages with available upgrades: `Microsoft.Extensions.Configuration` and `Microsoft.Extensions.Configuration.Json` from 9.0.0 → 10.0.10. Replace the deprecated `xunit` package in `SitefinityContentUpdater.Core.Tests` with its supported replacement. Three projects (`ImportJson`, `SitefinityUpdater.Core`, `SitefinityContentUpdater.Core.Tests`) have behavioral API change flags (ruleId=Api.0003) — query the assessment for detail on affected files and resolve any resulting compilation errors.

After updating all project files and packages, restore dependencies and do a single solution-wide build pass, fixing all compilation errors before completing the task.

**Done when**: All 6 projects target net10.0; solution builds with 0 errors and 0 warnings; `Microsoft.Extensions.Configuration*` packages are on 10.0.10; deprecated xunit replaced; all tests pass.
