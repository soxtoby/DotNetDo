# Glossary

## Solution navigation

### Solution

A `.sln` or `.slnx` file and its logical hierarchy. The default solution is the sole solution found in the nearest ancestor directory containing solution files; callers may instead identify one explicitly.

### Solution path

The canonical string identity of a project within a solution, formed from its containing solution folders and project name, separated by `/`. A root project uses only its project name; solution paths have no leading slash. Project lookup is ordinal case-sensitive, requires the complete solution path, and never falls back to a unique project name. The identity is virtual and independent of the project file's location on disk.

_Avoid_: Project path, disk path

### Solution project

A file-backed project entry in a solution. Solution items are not projects; projects unsupported by the available MSBuild toolset remain navigable but may not be evaluable.

## File-based app

A single C# source file intended to be created and run directly by DotNetDo.

Generated file-based apps reference the DotNetDo package and import the `DotNetDo` namespace by default.

The initial DotNetDo API surface is intentionally tiny. Generated apps reference it to establish a stable import path for future helpers.

## App name

The extensionless local name of a file-based app. In v1, an app name resolves only to `<app-name>.cs` in the current directory.

App names are simple file stems: letters, numbers, `_`, `-`, and `.` are allowed; path separators and a `.cs` suffix are rejected.

## App arguments

Arguments after the app name in a run command are forwarded to the file-based app.

## Script parameter

A file-based app argument declared by a literal DotNetDo API call. DotNetDo can discover script parameters by source scanning without executing the file-based app.

Script parameters resolve through DotNetDo's configuration pipeline: command-line arguments, `DOTNETDO_`-prefixed environment variables, user secrets, API default value, then `default`.

Script parameters are typed by the declared API call and parsed by DotNetDo from long command-line options.

App help is the DotNetDo-owned discovery surface for script parameters.

Script parameters may include an optional description for app help output.

Script parameter APIs return parameter wrappers. `.Required()` throws immediately when no value exists and otherwise returns a separate required wrapper so nullable flow matches runtime behavior. An immediate `.Required()` call marks a script parameter as always required for discovery; later `.Required()` calls are treated as conditional runtime validation.

## Secret value

A string script parameter value intended to avoid accidental clear-text output. Secret values require `Unwrap()` before use as plain text, render as redacted text, and missing secret parameters are represented as `null`.

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

Exec combines standard output and standard error into replayable `ExecOutput` objects containing an `Out` or `Error` type and a message. Their cross-pipe order is the order DotNetDo observes, not a guarantee of the external process's original write order.

Exec logs `Out` messages at `Information` and `Error` messages at `Error` by default. `ExecOptions.Log` is an optional action receiving each output type and raw message; apps may replace it to choose another logger or level. A missing or `null` action uses the default. Capture behavior is unchanged.

The default log action passes the raw message to the redacting logger. DotNetDo's redacting logger masks raw, JSON-escaped, and URI-escaped forms of resolved script-secret values, matching longer values first. Arbitrary transformations such as Base64 and hashes are outside the redaction guarantee.

## Logging bootstrap

DotNetDo's module-initializer setup of the process-wide logger for file-based apps. When Serilog still has its default silent logger, DotNetDo installs an `Information`-minimum logger using its CI log sink; the app remains free to replace `Log.Logger` normally. DotNetDo retains and disposes only its bootstrap logger at process exit, never a replacement owned by the app.

## Redacting logger

An `ILogger` wrapper created through `LoggerConfiguration.CreateRedactingLogger()`. It clones each log event and redacts resolved script secrets from message templates, exceptions, property names, and recursively nested property values before forwarding the event, following the complete-event approach demonstrated by `nblumhardt/serilog-redaction`. Contextual loggers returned by the wrapper remain wrapped. A replacement global logger is protected only when the app creates it through this extension.

## CI log sink

A Serilog sink exposed through `WriteTo.DefaultOutput()` that delegates to `Serilog.Sinks.Console` locally and writes build-agent-native commands on supported CI systems. It detects Azure Pipelines from `TF_BUILD=true`, GitHub Actions from `GITHUB_ACTIONS=true`, then falls back to the standard console sink. Warnings become CI warning annotations; errors and fatal events become CI error annotations; lower levels remain ordinary output. CI annotations contain only the rendered message and exception in v1, with no inferred source-file metadata. The sink changes log rendering and routing, not command execution.

## Additional arguments

Raw argument text appended after a configured tool command's structured argument slots.

## Tool command

A typed immutable record that describes an executable external tool command.

Tool commands use public `init` properties as their primary authored shape. Tool namespaces may expose default command instances as static fields so scripts can customize them with record `with` expressions.

Awaiting a tool command executes it through the Exec helper and requires a successful exit code.

Shared tool command option groups may be modeled as public non-generic base records when the underlying tool itself shares those options across commands.

## Argument slot

A tool command text fragment keyed by the concrete helper. The first use of a slot key fixes its command order, later uses of the same key replace the emitted text, and clearing an existing slot suppresses its text without changing surrounding slot order.

When multiple config properties write the same argument slot, the later write wins.

## Custom command

Raw positional command text used when a configured tool command has a known closed set of subcommands but must allow future or unsupported subcommands.
