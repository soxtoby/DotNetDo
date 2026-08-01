# Changelog

## Unreleased

## v0.6.0

### Added
- Non-generic `AbsolutePath` and `ExecResult` structured readers return native JSON, TOML, YAML, and XML document models; `AbsolutePath` can also write YAML and XML document models.
- `:init` can add recursively discovered task sources to a configured solution folder, synchronizing individual file entries for `.sln` and `.slnx`.
- `AbsolutePath.GetAncestry()` for traversing a path from itself through its root.
- C# tasks can declare an assembly-level `TaskDescription` shown in task lists, task help, and shell completion.
- `AbsolutePath` helpers for user profile, documents, application data, local application data, Program Files, and 32-bit Program Files directories.
- `Do.CreateTempDirectory()` and `Do.CreateTempFile()` for creating uniquely named temporary artifacts with optional prefixes and file extensions.

### Changed
- Default solution discovery now considers only `.sln` and `.slnx` files directly in the DotNetDo root; it no longer searches ancestor directories.
- Joining two `AbsolutePath` values with `/` is now rejected at compile time instead of binding through the implicit string conversion.
- `Do.Secret(...)` now returns `OptionalSecret`; `.Required()` and `new Secret(value)` produce a `Secret` with a non-nullable value.
- Bare `do` output now shows basic task invocation usage and a clearly headed task list.
- `AbsolutePath.Parent` and `RelativePath.Parent` are now non-nullable. Absolute roots throw on parent access; relative parents continue with `..` segments.

## v0.5.0

### Added
- User-scoped PowerShell, Bash, and Zsh completion through `dotnet-do :completion`, including built-in commands, task names, task parameters, booleans, and same-file enum values.
- `Tools.Bun` typed commands for dependency installation, scripts, tests, builds, and package publishing.
- `Tools.Bun.EnsureAvailable` for making Bun available through the shared tool-installation seam.
- `Tools.Npm` typed commands for installing dependencies, running scripts and tests, packing, and publishing packages.

### Changed
- Task help keeps parameter lines focused on descriptions, requirements, secrets, and defaults instead of derived environment variable names.
- Boolean task parameters accept a bare `--flag` as `true`; explicit `true` and `false` values remain supported.
- Name-only `Do.Param` declarations now return `OptionalParam<T>` and support typed parameters without defaults; defaults and `.Required()` both resolve to `Param<T>` with a non-nullable `Value`.
- Tool availability commands use the common `EnsureAvailable` property name: `Tools.Azure.EnsureAvailable`, `Tools.Bun.EnsureAvailable`, and `Tools.Scoop.EnsureAvailable`.
- Successful tool commands no longer emit a completion log after execution.

### Fixed
- Default process-output logging records each output line as the message instead of a structured log property.
- Task help renders named API default values without the `defaultValue:` argument name.

## v0.4.0

### Changed
- New task templates pin `DotNetDo.Core` to the running DotNetDo version.

### Added
- Camel, Pascal, snake, and kebab case string helpers.
- `AbsolutePath.RecreateDirectory()` for recreating an existing directory empty or creating it when missing.
- `Tools.Azure.Bicep` typed commands for Bicep authoring, modules, deployment snapshots, installation, and upgrades.
- `:update` for updating the root-local DotNetDo tool manifest entry and exact package pins in workspace scripts, with named-package, `--all`, and `--prerelease` modes.
- `Tools.DotNet.PackageSearch` and `Tools.DotNet.ToolUpdate` typed .NET CLI commands.
- `QuotedArgument()` extensions for optional and required task parameters and secrets.
- `ProjectInfo` and `Solution` string conversions returning their absolute file paths.

## v0.3.1

### Fixed
- Tasks no longer fail to read `dotnetdo.toml` with a Tomlyn reflection-serialization error. Configuration now loads through source-generated TOML metadata, and DotNetDo.Core re-enables reflection-based TOML and JSON serialization for consuming apps, which file-based apps otherwise disable through their Native AOT publishing defaults. `AbsolutePath.ReadJson`/`ReadToml` and their write counterparts therefore work in tasks without per-task `#:property` directives.

## v0.3.0

### Added
- `Do.IsLocalBuild` and CI-sensitive typed tool defaults, including `Release` configuration for .NET and MSBuild commands.
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
