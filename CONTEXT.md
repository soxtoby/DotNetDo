# Glossary

## Documentation

### Documentation site

The canonical public documentation for DotNetDo. Its sole published version describes `master`; it introduces the project, teaches repository automation workflows, and provides the complete DotNetDo.Core API reference.

### Documentation source

The committed inputs to the documentation build under `docs`: authored landing and Getting started pages, guides under `docs/guides`, API-family definitions under `docs/reference`, presentation overrides under `docs/template`, and `docs/docfx.json`.

### Generated documentation

Uncommitted API-page data and static site output under ignored `artifacts/docs`, derived from the current DotNetDo.Core public API and documentation source. Local builds and CI reproduce it.

### Getting started

The guide that takes a new DotNetDo user from installing the tool through initializing a workspace with `:init` to running its generated task. Shared instructions are shell-neutral; compact Windows and POSIX tabs show the differing generated-launcher commands. A complete compact version remains in README even though the documentation site is canonical.

### Documentation landing page

The basic site entry headed "Repository automation, in C#." It uses the README's project description, a two-line task example, and Get started, Documentation, and GitHub links without feature grids or promotional sections.

### Guide

An authored page that teaches a DotNetDo workflow or explains how its concepts fit together. The initial flat guide list covers task orchestration, Git, solution navigation, path values, and shell completion. Initialization belongs in Getting started; Azure and NuGet examples belong in API introductions. Tool-specific member documentation belongs in the API reference instead.

### API reference

A generated description of the complete public DotNetDo.Core API, organized into tool APIs and core APIs. Curated presentation may clarify facade types such as `Do` without changing their public contract. Project-defined inheritance and interface relationships remain visible; framework-root hierarchies, compiler-generated record members, and inherited boilerplate do not. Facade type cross-references resolve to their curated destination: a tool facade to its tool family and `Do` to the Core API overview. No duplicate facade page is generated.

### Reference introduction

An authored overview for one API family. It begins with the complete generated documentation for qualified entry points such as `Do.Exec()` and `Do.ApplicationData`, ordered by typical workflow, then links alphabetically ordered related generated type documentation. A related type has one canonical family even when other families also link it.

### API family

A documentation grouping for APIs used toward one task-authoring purpose, independent of their declaring C# type. Facade types such as `Do` supply entries to families but are not themselves families.

### Tool API

The part of the DotNetDo.Core API that models external command-line tools through `Tools`.

### Core API

The part of the DotNetDo.Core API outside tool APIs. It is presented by API family rather than by declaring type; `Do` is not a standalone reference category.

### Documentation navigation

The curated site hierarchy shown persistently in a left sidebar on desktop and as a drawer on smaller screens. Generated and unlisted pages retain this hierarchy rather than exposing a flat type inventory. API pages additionally provide a separate right-hand outline: family pages expose entry points and related types, while type pages expose member-kind sections and their alphabetically ordered members.

### Documentation search

The index of curated pages, canonical generated types, and public members. A member result opens its exact family or type anchor. Flat generated inventories and facade artifacts are excluded.

### Documentation theme

The shared landing-page and documentation presentation based on the Compiler notebook direction: compact technical typography, a cool light surface, deep blue primary color, and restrained red accents. Inter supplies interface and content text; JetBrains Mono supplies code, labels, and technical accents. Both load from Google Fonts rather than being stored in the repository. The theme follows the system color preference initially and offers a persistent manual light/dark override.

### Project README

The compact repository entry point containing the project description, installation, the complete Getting started flow, a minimal task example, and links to the canonical documentation site. Detailed guides and API documentation live only on the site.

### Documentation build

The reproducible local and CI operation that generates API data, composes API families, validates the documentation, and emits the static site. It reads an existing Release build of DotNetDo.Core rather than producing one, so the build task runs first both locally and in CI. `./do docs` runs it locally; `./do docs --serve` additionally serves the result without live rebuilding. `./do docs --site-only` reuses previously generated API data, which suits iterating on documentation source while a server started separately keeps serving the rebuilt output.

### Documentation validation

The CI gate that rejects generation errors, broken internal links or cross-references, duplicate API ownership, and undocumented public APIs. Any tolerated Docfx warning requires an explicit suppression.

### Documentation URL

The public site at `https://soxtoby.github.io/DotNetDo/`. All generated paths and assets remain base-path-safe so a later custom domain does not require content changes.

## Document model

A format-native, navigable representation of a structured document that does not require a caller-defined value type. A document-model reader complements, rather than replaces, typed deserialization.

## Workspace

### DotNetDo configuration

A committed `dotnetdo.toml` file containing shared configuration for tasks. Its containing directory establishes the DotNetDo root directory. Top-level keys, including the closed `tools` array of logical tool requirement names, and the `tasks` table are owned by DotNetDo; other tables are parameter namespaces. Unknown top-level keys, unknown or duplicate tool names, and other invalid configuration fail operations that require configuration; values never silently fall back.

### Root directory

The nearest ancestor of the initial working directory containing DotNetDo configuration, or that working directory when none exists. The resolved root remains stable for the process.

### Working directory

The current process working directory from which a task operates. It may change during execution; reads reflect its current value, and assignment changes it process-wide.

### Scripts path

The root-relative path containing DotNetDo scripts. It defaults to `scripts` and may be configured with the top-level `scripts-path` key in DotNetDo configuration; `.` selects the root directory. Empty, absolute, and root-escaping values are invalid. Containment is lexical; symbolic links retain normal filesystem behavior.

### Default solution path

The root-relative path of the solution used by default. When `solution-path` is configured it is authoritative; otherwise DotNetDo discovers the default solution.

## Git

### Git repository

A repository bound to a discovered working-tree root. Repository operations remain rooted there even if the process working directory later changes; construction from a path discovers its containing repository and fails when none exists. The default repository is discovered from the DotNetDo root directory. Its root is stable, while branch, commit, and working-tree information always reflects current repository state.

### Repository verification

An operation guarded by a before-and-after comparison of Git-visible file state. It passes when the final state equals the captured baseline; pre-existing changes are allowed.

_Avoid_: Clean repository check, generated-code check

## Solution navigation

### Solution

A `.sln` or `.slnx` file and its logical hierarchy. The default solution is the sole solution found directly in the DotNetDo root directory; callers may instead identify one explicitly.

### Solution path

The canonical string identity of a project within a solution, formed from its containing solution folders and project name, separated by `/`. A root project uses only its project name; solution paths have no leading slash. Project lookup is ordinal case-sensitive, requires the complete solution path, and never falls back to a unique project name. The identity is virtual and independent of the project file's location on disk.

_Avoid_: Project path, disk path

### Solution project

A file-backed project entry in a solution. Solution items are not projects; projects unsupported by the available MSBuild toolset remain navigable but may not be evaluable.

### Evaluated project

A live, mutable MSBuild representation of a solution project's file for a particular set of global properties.

_Avoid_: Loaded project

### Project name

The name assigned to a solution project. It is the final segment of the solution path and may differ from the project filename or assembly name.

_Avoid_: Project display name

### Scripts solution folder

The configured root solution folder presenting every C# source beneath the scripts path. Its virtual name is independent of the scripts path and changing it does not remove an older solution folder.

## Task

A named runnable unit discovered by DotNetDo. A task is either implemented as a single C# source file or defined as a meta-task in DotNetDo configuration.

Task parameters are named values before the `--` command-line separator. Task arguments are the ordered, unchanged values after the separator and are available through `Do.TrailingArguments`. Values after the separator are never interpreted as task parameters.

Generated tasks pin the latest stable DotNetDo.Core version available from configured NuGet sources and import the `DotNetDo` namespace by default.

The initial DotNetDo API surface is intentionally tiny. Generated tasks reference it to establish a stable import path for future helpers.

## Meta-task

A task defined under the `tasks` table in DotNetDo configuration. A string value defines one task invocation; a string array defines an ordered sequence of task invocations. Each invocation parses its first token as the task name and decodes the remaining command-line text into values, preserving quoted spaces, escaped quotes, empty values, and backslashes.

Each invocation may supply fixed task parameters and trailing arguments. Values supplied to the meta-task are inherited by every invocation. Inherited parameters precede fixed parameters, giving fixed parameters precedence; inherited trailing arguments precede fixed trailing arguments. Parameters remain before the `--` separator. Invocations run sequentially; the first failure stops execution and becomes the meta-task result. Meta-tasks have no cleanup or finally phase.

Argument inheritance is unconditional. Invoked tasks using custom argument parsing must tolerate inherited arguments that may primarily concern sibling tasks.

Meta-tasks express scenario-specific composition; DotNetDo does not track prerequisite satisfaction, infer freshness, or deduplicate task executions.

Meta-task composition is static. It has no conditions, parallel execution, environment filters, or library API for dynamically invoking discovered tasks; complex automation remains C# task logic.

DotNetDo resolves and traverses nested meta-tasks within the current tool process. Each invoked C# task still runs in a separate process with the same behavior as direct task execution; meta-task nesting does not recursively launch the DotNetDo tool.

Configured meta-tasks and C# task files share one task-name namespace. Defining both with the same name is invalid configuration; neither representation takes precedence.

Meta-tasks may invoke other meta-tasks. Every configured invocation must resolve to a task, and the complete configured graph must be acyclic; invalid references or cycles fail configuration loading before any task executes.

A meta-task must contain at least one invocation. Empty arrays and empty or whitespace-only invocation strings are invalid configuration.

Task help for a meta-task displays its authored invocation sequence and explains argument forwarding. It does not merge or infer parameter declarations from invoked tasks.

## Release automation

### Release preparation

A local, reviewable transition of Unreleased changelog entries into a versioned release. It updates the package version and advances automation to the current published DotNetDo version, but does not commit, tag, push, or publish.

### Release

A tagged, immutable package version whose project version, changelog heading, Git tag, NuGet package, and GitHub release share the same `v`-prefixed identity, except that NuGet's package version omits the prefix.

## Task name

The unique name of a C# task or configured meta-task. A C# task name resolves only to `<scripts-path>/<task-name>.cs`; nested directories are not searched.

Task names use one grammar across both representations: letters, numbers, `_`, `-`, and `.` are allowed; path separators, a `.cs` suffix, and a leading `:` are rejected.

## Task arguments

Arguments after the task name in a run command are forwarded to the task.

## Pinned package

A script package directive with one exact version, including an exact prerelease version. Missing and floating versions are not pins and remain unchanged by package updates. Updates never select an older semantic version. Stable releases are candidates by default; prereleases are candidates only when explicitly requested, while an existing prerelease pin may advance to its newer stable release.

## Task parameter

A task argument declared by a literal DotNetDo API call. DotNetDo can discover task parameters by source scanning without executing the task.

Task parameters resolve through DotNetDo's configuration pipeline: command-line arguments, `DOTNETDO_`-prefixed environment variables, user secrets, committed DotNetDo configuration, API default value, then `default`.

Task parameters are typed by the declared API call and parsed by DotNetDo from long command-line options.

Task help is the DotNetDo-owned discovery surface for task parameters.

Task parameters may include an optional description for task help output.

`Do.Param(name)` and `Do.Param<T>(name)` declare optional parameters with nullable values. `.Required()` throws immediately when no value exists and otherwise returns a non-nullable `Param<T>`; an immediate call marks the parameter as always required for discovery, while a later call is conditional runtime validation. `Do.Param(name, defaultValue, description)` requires a non-null default and also returns `Param<T>`; a defaulted parameter is never required input.

At the start of an interactive local run command, DotNetDo discovers immediate `.Required()` declarations and resolves cheap configured sources before evaluating a file-based task's user-secrets identity. It prompts on standard output for values still missing and forwards each answer only to child invocations where that value was unresolved; prompted secrets use the child environment rather than command-line arguments. Failure to inspect a child's user secrets defers resolution to that child. An undiscovered runtime `.Required()` also prompts inside its C# task after every configured source fails to supply a value; runtime answers are local to the child process.

Invalid typed input reports the parse failure and prompts again. Boolean prompts additionally accept `y`, `yes`, `n`, and `no`. Blank input is valid for strings and secrets. Secret input is masked. Closed input fails with the ordinary required-parameter error; console interruption aborts normally.

A bare long option resolves to `true` for a Boolean parameter. Other parameter types require an explicit value.

## Secret value

A string value intended to avoid accidental clear-text output. `Do.Secret(...)` returns an `OptionalSecret`; `.Required()` resolves it to a `Secret`. Callers may also construct a known value directly with `new Secret(value)`. Resolved values register with DotNetDo's redacting logger and the native masking command of every active CI provider. Secret values require `Unwrap()` before use as plain text, render as redacted text, and only optional secret parameters unwrap to `null`.

## Run command

The run command executes a task through SDK file execution, equivalent to `dotnet <task-name>.cs -- <task-arguments>`. DotNetDo does not emulate file-based task support for older SDKs.

## Task list

Running `./do` with no arguments shows basic task invocation usage followed by a headed, indented list of C# tasks directly inside the scripts path and meta-tasks from DotNetDo configuration together, alphabetically and without representation markers. C# tasks may declare a concise, statically discoverable description with `[assembly: TaskDescription("...")]`; it appears in task lists, task help, and shell completion. Nested directories are not searched. A missing scripts path produces only configured meta-tasks.

## New command

The `:new` command creates a task directly inside the scripts path and fails if the target file already exists. It creates a missing scripts path. When both `solution-path` and `solution-folder` are configured, it resynchronizes the owned solution folder.

On Unix-like systems, `:new` makes the generated file executable on a best-effort basis. Windows does not need executable bits for DotNetDo usage.

## Rename command

The `:rename` command renames a C# task directly inside the scripts path without changing its contents. It fails when the source does not exist or the target file or meta-task name is occupied. When both `solution-path` and `solution-folder` are configured, it resynchronizes the owned solution folder.

## Init command

The `:init` command interactively creates a DotNetDo workspace in the current directory: committed configuration, a scripts path, and an initial task. It may select a default solution and may create a nested workspace only after warning about the containing workspace.

Initialization also creates root-local `do.cmd` and executable `do` launchers which forward all arguments to `dnx DotNetDo`.

## Global command

The installed `dotnetdo` command bootstraps DotNetDo workspaces and manages user-scoped shell completion. Commands within an initialized workspace use its `./do` launcher.

## Install command

The `:install` command executes the install plan for every tool requirement declared in DotNetDo configuration. It takes no arguments; installing an individual tool is the job of that tool's own tool install. An empty or absent declaration set succeeds trivially.

## Update command

The `:update` command updates the root-local manifest's DotNetDo tool and pinned DotNetDo.Core packages in scripts. A package name selects only that script package instead; `--all` selects every pinned script package and still updates the DotNetDo tool. `--prerelease` admits prerelease candidates for the selected packages.

## Tool command

A command whose name starts with `:` is owned by DotNetDo. Task names cannot start with `:`.

DotNetDo v1 includes workspace initialization with `:init`, task listing, task creation with `:new`, task renaming with `:rename`, help with `:help`, shell completion setup with `:completion`, tool requirement installation with `:install`, and task execution by name.

## Shell completion

User-scoped shell integration that completes DotNetDo task names and their discoverable parameters. Bare `:completion` installs it for the current shell; completion remains separate from workspace tool installation.

Completion installation requires the `dotnetdo` global command on `PATH` and fails before changing shell state when it is unavailable; uninstall remains available. Both `dotnetdo` and `./do` receive completion candidates.

## Exec helper

A DotNetDo library helper for running an external program from a task.

Exec helper commands are a single command-line string where DotNetDo parses only the program token and passes the remaining argument string to .NET process execution.

Exec combines standard output and standard error into replayable `ExecOutput` objects containing an `Out` or `Error` type and a message. Their cross-pipe order is the order DotNetDo observes, not a guarantee of the external process's original write order.

Exec logs `Out` messages at `Information` and `Error` messages at `Error` by default. `ExecOptions.Log` accepts an `ExecOutputLog`; its discoverable choices include `Default`, `None`, `ErrorsOnly`, filtering an existing choice, and constructing custom per-line behavior. A missing or `null` value uses `Default`. Capture behavior is unchanged.

The default log action passes the raw message to the redacting logger. DotNetDo's redacting logger masks raw, JSON-escaped, and URI-escaped forms of registered `Secret` values, matching longer values first. Arbitrary transformations such as Base64 and hashes are outside the redaction guarantee.

## Fetch helper

A DotNetDo library helper for sending an HTTP request from a task. It is the HTTP counterpart to the Exec helper: the request is sent immediately, awaiting it requires a successful status code, and `Completed` returns the buffered response for any status.

Format shortcuts such as `Do.FetchJson` request their format with an `Accept` header unless the caller supplies one, require success, and return the parsed body. `Do.FetchFile` streams the body to disk and never buffers it.

_Avoid_: Download helper, HTTP client

## HTTP tool command

A typed immutable record that describes a request to an HTTP API, the HTTP counterpart to a tool command. It derives its URL from its own properties and carries its own method, headers, and body. Awaiting it produces a semantic result; passing it to the Fetch helper returns the raw response. HTTP tool commands are internal until DotNetDo ships an HTTP-based tool.

## Logging bootstrap

DotNetDo's module-initializer setup of the process-wide logger for tasks. When Serilog still has its default silent logger, DotNetDo installs a logger using its CI log sink and `Logging.Level`, which defaults to `Information`; the task remains free to replace `Log.Logger` normally. `Logging.Level` remains DotNetDo's explicit task-wide output-volume preference when the logger is replaced, and fresh typed tool commands snapshot it into best-effort native volume controls. DotNetDo retains and disposes only its bootstrap logger at process exit, never a replacement owned by the task.

## Redacting logger

An `ILogger` wrapper created through `LoggerConfiguration.CreateRedactingLogger()`. It clones each log event and redacts registered secrets from message templates, exceptions, property names, and recursively nested property values before forwarding the event, following the complete-event approach demonstrated by `nblumhardt/serilog-redaction`. Contextual loggers returned by the wrapper remain wrapped. A replacement global logger is protected only when the app creates it through this extension.

## CI log sink

A Serilog sink exposed through `WriteTo.DefaultOutput()` that delegates to `Serilog.Sinks.Console` locally and writes build-agent-native commands on supported CI systems. Verbose and debug events become native debug messages, warnings become warning annotations, and errors or fatal events become error annotations for every detected provider. Information remains one ordinary output line even when both providers are detected. CI annotations contain only the rendered message and exception in v1, with no inferred source-file metadata. The sink changes log rendering and routing, not command execution.

## Runner command

A control message written by a task to the active CI build agent, such as setting an output or opening a log group. Runner commands exclude service CLIs such as `gh` and `az`.

_Avoid_: CI server tool, workflow command

## Provider-native runner API

A public API modeling one CI provider's runner commands and build metadata with that provider's own semantics. `Do.GitHubActions` and `Do.AzurePipelines` remain separate APIs; `GitHub` is reserved for the `gh` CLI. Shared infrastructure does not imply a portable command surface. Each internally resolved singleton is available only on its detected host and is otherwise `null`; its typed, provider-grouped metadata is snapshotted when resolved.

_Avoid_: Universal CI API, provider-neutral command

## Local build

A build running without an active supported CI provider and without a truthy conventional `CI` marker. `Do.IsLocalBuild` exposes this distinction to tasks; an unsupported CI host with `CI=true` is therefore not local.

_Avoid_: Build environment, non-CI build

## Interactive local build

A local build whose process has an interactive console for both input and output. Missing required task parameters may prompt only during an interactive local build; other builds fail instead.

_Avoid_: Local build

## CI build default

A tool setting inferred when the current build is not a local build. Build configuration defaults to `Release`; a local build infers no configuration and leaves the tool's native default authoritative. Each tool family owns its equivalent mapping.

## Additional arguments

Raw argument text appended after a configured tool command's structured argument parts.

## Structured tool argument

A semantic value exposed by a tool command's typed properties. Property getters preserve that authored value; the owning tool command renders it as one or more command-line-safe arguments. Callers do not pre-format or quote it.

Each concrete tool command defines one canonical order for its structured arguments. Rendering is independent of property assignment order.

Each element of a structured argument list represents exactly one command-line argument and is rendered independently. Intentional multi-argument syntax belongs in additional arguments.

Canonical command ordering determines where a structured argument collection is rendered, but preserves that collection's authored iteration order.

Concrete commands snapshot caller-owned structured argument collections so later external mutation cannot change the command value.

When a tool command combines values into one structured argument, it constructs the complete semantic argument before rendering and quoting it once.

## Package tool

A typed external-tool definition associated with a package ID and command name. The local tool manifest in scope from the DotNetDo root owns its version; raw execution verifies declaration and diagnoses missing restoration, while semantic awaiting may restore and retry an unavailable declared tool.

Awaiting a value-producing package tool returns its semantic result. Passing the same value to the Exec helper bypasses result parsing and exposes the raw process result.

_Avoid_: Installed-tool wrapper, dotnet tool wrapper

## Tool install

An explicit awaitable operation that makes an external tool available in the host environment. It may coordinate multiple external commands and environment refreshes; executing a tool command never implicitly invokes its tool install.

A tool install succeeds without changing the host when its tool requirement is already satisfied. When installation infrastructure is missing, the tool install runs the required installer bootstrap itself, announcing it in log output rather than failing.

_Avoid_: Install command, automatic tool installation, tool prerequisite

## Installer bootstrap

An explicit installation step that makes a platform installer available when no installer can own that installation. Platform installers are installation infrastructure, not workspace tool requirements; their bootstrap follows the current official installer rather than promising a reproducible version. Under an elevated process, the bootstrap opts into the installer's official elevated mode instead of failing.

_Avoid_: Pinned installer, installer package tool

## Tool requirement

A logical external tool declared by canonical name in DotNetDo configuration. The canonical name is the lowercase name of the tool's own API namespace, never a platform package name, because package names differ across operating systems. It is satisfied when its executable command is available regardless of installation provenance, and guarantees availability rather than a version; platform installer package mappings are DotNetDo-owned.

_Avoid_: Scoop package, installer dependency

## Install plan

The complete platform-specific resolution of missing tool requirements and required installation infrastructure. Every requirement must resolve before the plan may change the host environment; execution is sequential, stops at the first failure, does not roll back host state, and refreshes the current process environment after successful installation without changing ordinary startup behavior.

_Avoid_: Partial install, best-effort setup

## Tool command

A typed immutable record that describes an executable external tool command.

Each concrete tool command owns the command-line rendering and canonical order of its structured tool arguments. Shared rendering helpers quote semantic values by default; a concrete command explicitly renders raw syntax. Additional arguments remain raw.

Structured argument values live in the concrete command's ordinary properties. The shared tool-command base stores no structured argument state.

Command parts include the executable, subcommand, execution context, and property-derived arguments in their complete canonical order.

Tool commands use public `init` properties as their primary authored shape. Tool namespaces may expose default command instances as static fields so scripts can customize them with record `with` expressions.

Tool commands carry their own process working directory, child-environment transformation, and output logging configuration. Raw command strings use separate Exec options because no command value exists to own that configuration.

Fresh tool commands snapshot `Logging.Level` into dedicated native output-volume controls. Explicit typed values override or clear only their own control; raw additional arguments remain opaque.

## NuGet command suite

The typed DotNetDo command surface for current, documented `nuget.exe` task automation. It excludes the help command and executable self-update, while modeling every other non-deprecated command and option with a current official reference page.

_Avoid_: NuGet package tool, `dotnet nuget`

`Tools.Git` exposes default Git command values bound lazily through `Do.GitRepo`; a specific Git repository exposes equivalent values permanently bound to its root.

Awaiting a tool command executes it through the Exec helper and requires a successful exit code.

Shared tool command option groups may be modeled as public non-generic base records when the underlying tool itself shares those options across commands.

## Custom command

Raw positional command text used when a configured tool command has a known closed set of subcommands but must allow future or unsupported subcommands.

When supplied alongside the typed command selection, the custom command deterministically controls rendering regardless of property assignment order.

## Test reporter

A test runner's own extension that writes a report file, such as TRX or JUnit XML, or native CI output, such as GitHub Actions annotations and step summaries, during a test run. DotNetDo configures test reporters through test tool commands; it never produces, parses, or publishes test reports itself.

_Avoid_: Test result publishing, test tool, CI server tool

## Test runner mode

The runner a repository selects for `dotnet test`: VSTest by default, or Microsoft.Testing.Platform when opted into through `global.json` or `DOTNET_TEST_RUNNER`. It is a property of the repository, not of an individual command; each mode accepts a different set of test options.

_Avoid_: TestPlatform (VSTest's own package name), MTP mode
