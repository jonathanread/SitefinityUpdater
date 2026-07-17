# 01-prerequisites: Verify SDK and toolchain for net10.0

Confirm the .NET 10 SDK is installed and that any `global.json` in the repository is compatible with net10.0. This is a fast, non-destructive check that gates all subsequent work. If `global.json` pins an older SDK version, it must be updated to allow the net10.0 SDK before any project files are changed.

**Done when**: `dotnet --version` returns a .NET 10 SDK; any `global.json` allows net10.0 SDK; solution restores without SDK version errors.
