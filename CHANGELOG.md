# Changelog

## Unreleased

### Added
- `do` CLI tool for creating, listing, and running tasks.
- `Do.Exec` function for easily running console commands.
- `Do.Param` and `Do.Secret` functions for easily reading configuration values and secrets.
- Serilog logging for simple logging that integrates with CI providers and automatically redacts secret values.
- `AbsolutePath` and `RelativePath` types for working with file paths.
- Helpers for parsing and serializing JSON, XML, and TOML files.
- Azure Pipelines and GitHub Actions helpers.
- `Do.GitRepo` for accessing information about the current repo.
- `Do.Solution` for accessing information about the current solution and parsing project files.
- `Tools.DotNet.*` tools for running dotnet CLI.
- `Tools.Git` tools for running git commands.
- `Tools.GitVersion` tool for running GitVersion.