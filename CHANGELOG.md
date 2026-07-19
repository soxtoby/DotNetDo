# Changelog

## Unreleased

### Added
- Configured meta-tasks in `dotnetdo.toml` for composing ordered, fail-fast task sequences with argument forwarding.
- `Tools.DotNet.NuGetPush` for publishing NuGet packages.
- `Tools.MSBuild` for running the MSBuild toolset discovered by MSBuild Locator.
- `Tools.VSTest` for running test containers with the VSTest runner discovered alongside the installed MSBuild toolset.

### Changed
- `Logging.Level` now supplies best-effort native output-volume defaults to fresh typed tool commands, with explicit per-tool controls taking precedence.
- `Do.Secret` now returns `Secret`, `new Secret(value)` automatically registers the value for redaction, and the required wrapper is now named `RequiredSecret`.
- Typed tool commands now quote structured argument values automatically while preserving raw additional arguments.
- `ToolCommand` supports integer arguments with invariant formatting.
- `ToolCommand` centralizes lossless dictionary argument storage and rendering.

## v0.2.0

### Changed
- Changed tool command to `dotnet-do` with workspace-local `do.cmd` and `do` launchers created by `:init`. 
- `GitRepository.IsDirty` ignores ignored files.

## v0.1.0

### Added
- `do` CLI tool for creating, listing, and running tasks.
- `Do.Exec` function for easily running console commands.
- `Do.Param` and `Do.Secret` functions for easily reading configuration values and secrets.
- Serilog logging for simple logging that integrates with CI providers and automatically redacts secret values.
- `AbsolutePath` and `RelativePath` types for working with file paths.
- Helpers for parsing and serializing JSON, TOML, YAML, and XML files.
- Azure Pipelines and GitHub Actions helpers.
- `Do.GitRepo` for accessing information about the current repo.
- `Do.Solution` for accessing information about the current solution and parsing project files.
- `Tools.DotNet.*` tools for running dotnet CLI.
- `Tools.Git` tools for running git commands.
- `Tools.GitVersion` tool for running GitVersion.
