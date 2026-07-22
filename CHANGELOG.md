# Changelog

## Unreleased

### Added
- `tools` array in `dotnetdo.toml` for declaring tool requirements by canonical name, installed on demand with the `:install` command.
- `Tools.Azure.Install` for making the Azure CLI available, installing it through Scoop (and bootstrapping Scoop itself) when missing.
- `Tools.Scoop` commands for installing, uninstalling, and updating apps, managing buckets and settings, plus `Tools.Scoop.InstallSelf` for bootstrapping Scoop with the official installer.
- `AbsolutePath.TryParse()` for converting textual input that may not be a valid absolute path.
- `RelativePath.QuotedArgument()` for interpolating relative paths into raw command strings, matching `AbsolutePath`.
- Scripting convenience extensions for sequence joining and filtering, line splitting, nullable string checks, and receiver-style regular expressions.
- Configured meta-tasks in `dotnetdo.toml` for composing ordered, fail-fast task sequences with argument forwarding.
- `Tools.DotNet.NuGetPush` for publishing NuGet packages.
- `Tools.MSBuild` for running the MSBuild toolset discovered by MSBuild Locator.
- `Tools.VSTest` for running test containers with the VSTest runner discovered alongside the installed MSBuild toolset.

### Changed
- `Do.Exec` now resolves commands through `PATH` and invokes Windows batch shims through their native command host, so typed tools no longer need launcher-specific execution overrides.
- `ToolInstall.IsAvailable` reports executable availability; installs may omit a Scoop package, failing clearly when no installer is configured.
- `Logging.Level` now supplies best-effort native output-volume defaults to fresh typed tool commands, with explicit per-tool controls taking precedence.
- `Do.Secret` now returns `Secret`, `new Secret(value)` automatically registers the value for redaction, and the required wrapper is now named `RequiredSecret`.
- Typed tool commands now quote structured argument values automatically while preserving raw additional arguments.
- Typed tool commands now own semantic property state and canonical argument order, independent of property assignment order. `ToolCommand` replaces its protected keyed argument setter/getter API with `Arg` and `Args` rendering helpers.

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
