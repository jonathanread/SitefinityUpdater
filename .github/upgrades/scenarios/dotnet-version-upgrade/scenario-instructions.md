# .NET Version Upgrade

## Preferences
- **Flow Mode**: Automatic
- **Target Framework**: net10.0 (.NET 10.0 LTS)

## Source Control
- **Source Branch**: master
- **Working Branch**: dotnet-version-upgrade-1
- **Commit Strategy**: After Each Task

## Upgrade Options
**Source**: .github/upgrades/scenarios/dotnet-version-upgrade/upgrade-options.md

### Strategy
- Upgrade Strategy: All-at-Once

## Strategy
**Selected**: All-at-Once
**Rationale**: 6 projects, all on modern .NET (net9.0/net10.0), 2-tier shallow dependency graph — single atomic pass is lowest overhead.

### Execution Constraints
- Single atomic upgrade — all projects updated together in one pass
- Validate full solution build after all project and package changes are applied
- Fix all compilation errors in a single bounded pass (not a retry loop)
- Tests run only after the solution builds successfully with 0 errors
- No tier ordering — all projects are upgraded simultaneously
