# Glossary

## Workspace

### DotNetDo configuration

A committed `dotnetdo.toml` file containing shared configuration for scripts. Its containing directory establishes the DotNetDo root directory. Top-level keys are owned by DotNetDo; tables are reserved for parameter namespaces. Unknown top-level keys and invalid configuration fail operations that require configuration; values never silently fall back.

### Root directory

The nearest ancestor of the current working directory containing DotNetDo configuration. Without configuration, the current working directory is the root directory and discovery is retried whenever requested; once configuration is found, that root remains stable.

### Working directory

The current process working directory from which a file-based app operates. It may change during execution; reads reflect its current value, and assignment changes it process-wide.

### Scripts path

The root-relative path containing DotNetDo scripts. It defaults to `scripts` and may be configured with the top-level `scripts-path` key in DotNetDo configuration; `.` selects the root directory. Empty, absolute, and root-escaping values are invalid. Containment is lexical; symbolic links retain normal filesystem behavior.

## Git

### Git repository

A repository bound to a discovered working-tree root. Repository operations remain rooted there even if the process working directory later changes; construction from a path discovers its containing repository and fails when none exists. The default repository is discovered from the DotNetDo root directory. Its root is stable, while branch, commit, and working-tree information always reflects current repository state.

## Solution navigation

### Solution

A `.sln` or `.slnx` file and its logical hierarchy. The default solution is the sole solution found in the nearest ancestor of the DotNetDo root directory containing solution files; callers may instead identify one explicitly.

### Solution path

The canonical string identity of a project within a solution, formed from its containing solution folders and project name, separated by `/`. A root project uses only its project name; solution paths have no leading slash. Project lookup is ordinal case-sensitive, requires the complete solution path, and never falls back to a unique project name. The identity is virtual and independent of the project file's location on disk.

_Avoid_: Project path, disk path

### Solution project

A file-backed project entry in a solution. Solution items are not projects; projects unsupported by the available MSBuild toolset remain navigable but may not be evaluable.

## File-based app

A single C# source file intended to be created and run directly by DotNetDo.

Generated file-based apps reference the DotNetDo package and import the `DotNetDo` namespace by default.

The initial DotNetDo API surface is intentionally tiny. Generated apps reference it to establish a stable import path for future helpers.

## Script

A directly runnable unit discovered by DotNetDo, currently implemented as a file-based app. A future task may orchestrate one or more scripts.

## App name

The extensionless name of a script. An app name resolves only to `<scripts-path>/<app-name>.cs`; nested directories are not searched.

App names are simple file stems: letters, numbers, `_`, `-`, and `.` are allowed; path separators and a `.cs` suffix are rejected.

## App arguments

Arguments after the app name in a run command are forwarded to the file-based app.

## Script parameter

A file-based app argument declared by a literal DotNetDo API call. DotNetDo can discover script parameters by source scanning without executing the file-based app.

Script parameters resolve through DotNetDo's configuration pipeline: command-line arguments, `DOTNETDO_`-prefixed environment variables, user secrets, committed DotNetDo configuration, API default value, then `default`.

Script parameters are typed by the declared API call and parsed by DotNetDo from long command-line options.

App help is the DotNetDo-owned discovery surface for script parameters.

Script parameters may include an optional description for app help output.

Script parameter APIs return parameter wrappers. `.Required()` throws immediately when no value exists and otherwise returns a separate required wrapper so nullable flow matches runtime behavior. An immediate `.Required()` call marks a script parameter as always required for discovery; later `.Required()` calls are treated as conditional runtime validation.

## Secret value

A string script parameter value intended to avoid accidental clear-text output. Resolving one registers it with DotNetDo's redacting logger and the native masking command of every active CI provider. Secret values require `Unwrap()` before use as plain text, render as redacted text, and missing secret parameters are represented as `null`.

## Run command

The run command executes a file-based app through SDK file execution, equivalent to `dotnet <app-name>.cs -- <app-arguments>`. DotNetDo does not emulate file-based app support for older SDKs.

## App list

Running `do` with no arguments lists scripts directly inside the scripts path. Nested directories are not searched. A missing scripts path produces an empty list.

## New command

The `:new` command creates a script directly inside the scripts path and fails if the target file already exists. It creates a missing scripts path.

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

A Serilog sink exposed through `WriteTo.DefaultOutput()` that delegates to `Serilog.Sinks.Console` locally and writes build-agent-native commands on supported CI systems. Verbose and debug events become native debug messages, warnings become warning annotations, and errors or fatal events become error annotations for every detected provider. Information remains one ordinary output line even when both providers are detected. CI annotations contain only the rendered message and exception in v1, with no inferred source-file metadata. The sink changes log rendering and routing, not command execution.

## Runner command

A control message written by a file-based app to the active CI build agent, such as setting an output or opening a log group. Runner commands exclude service CLIs such as `gh` and `az`.

_Avoid_: CI server tool, workflow command

## Provider-native runner API

A public API modeling one CI provider's runner commands and build metadata with that provider's own semantics. `Do.GitHubActions` and `Do.AzurePipelines` remain separate APIs; `GitHub` is reserved for the `gh` CLI. Shared infrastructure does not imply a portable command surface. Each internally resolved singleton is available only on its detected host and is otherwise `null`; its typed, provider-grouped metadata is snapshotted when resolved.

_Avoid_: Universal CI API, provider-neutral command

## Additional arguments

Raw argument text appended after a configured tool command's structured argument slots.

## Package tool

A typed external-tool definition associated with a package ID and command name. The local tool manifest in scope from the DotNetDo root owns its version; raw execution verifies declaration and diagnoses missing restoration, while semantic awaiting may restore and retry an unavailable declared tool.

Awaiting a value-producing package tool returns its semantic result. Passing the same value to the Exec helper bypasses result parsing and exposes the raw process result.

_Avoid_: Installed-tool wrapper, dotnet tool wrapper

## Tool command

A typed immutable record that describes an executable external tool command.

Tool commands use public `init` properties as their primary authored shape. Tool namespaces may expose default command instances as static fields so scripts can customize them with record `with` expressions.

Tool commands carry their own process working directory and output logging configuration. Raw command strings use separate Exec options because no command value exists to own that configuration.

`Tools.Git` exposes default Git command values bound lazily through `Do.GitRepo`; a specific Git repository exposes equivalent values permanently bound to its root.

Awaiting a tool command executes it through the Exec helper and requires a successful exit code.

Shared tool command option groups may be modeled as public non-generic base records when the underlying tool itself shares those options across commands.

## Argument slot

A tool command text fragment keyed by the concrete helper. The first use of a slot key fixes its command order, later uses of the same key replace the emitted text, and clearing an existing slot suppresses its text without changing surrounding slot order.

When multiple config properties write the same argument slot, the later write wins.

## Custom command

Raw positional command text used when a configured tool command has a known closed set of subcommands but must allow future or unsupported subcommands.
