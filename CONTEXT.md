# Glossary

## File-based app

A single C# source file intended to be created and run directly by DotNetDo.

Generated file-based apps reference the DotNetDo package and import the `DotNetDo` namespace by default.

The initial DotNetDo API surface is intentionally tiny. Generated apps reference it to establish a stable import path for future helpers.

## App name

The extensionless local name of a file-based app. In v1, an app name resolves only to `<app-name>.cs` in the current directory.

App names are simple file stems: letters, numbers, `_`, `-`, and `.` are allowed; path separators and a `.cs` suffix are rejected.

## App arguments

Arguments after the app name in a run command are forwarded to the file-based app.

## Run command

The run command executes a file-based app through SDK file execution, equivalent to `dotnet <app-name>.cs -- <app-arguments>`. DotNetDo does not emulate file-based app support for older SDKs.

## App list

Running `do` with no arguments lists file-based apps in the current directory.

## New command

The `:new` command creates a file-based app and fails if the target file already exists.

On Unix-like systems, `:new` makes the generated file executable on a best-effort basis. Windows does not need executable bits for DotNetDo usage.

## Tool command

A command whose name starts with `:` is owned by DotNetDo. App names cannot start with `:`.

DotNetDo v1 includes only app listing, app creation with `:new`, help with `:help`, and app execution by name.

## Exec helper

A DotNetDo library helper for running an external program from a file-based app.

Exec helper commands are a single command-line string where DotNetDo parses only the program token and passes the remaining argument string to .NET process execution.

## Additional arguments

Raw argument text appended after a configured tool command's structured argument slots.

## Configured tool command

A DotNetDo library helper that creates a typed configuration object, applies a configuration callback, builds command text, and executes it through the Exec helper.

Configured tool commands use immutable config records with public `init` properties as their primary authored shape.

Shared configured tool command option groups may be modeled as public non-generic base records when the dotnet CLI itself shares those options across commands.

## Argument slot

A configured tool command text fragment keyed by the concrete helper. The first use of a slot key fixes its command order, later uses of the same key replace the emitted text, and clearing an existing slot suppresses its text without changing surrounding slot order.

When multiple config properties write the same argument slot, the later write wins.

## Custom command

Raw positional command text used when a configured tool command has a known closed set of subcommands but must allow future or unsupported subcommands.
