# Getting started

Create and run your first repository-local DotNetDo task.

## Requirements

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

## Install DotNetDo

```console
dotnet tool install --global DotNetDo
```

Install task and parameter completion for PowerShell, Bash, or Zsh, then restart your shell:

```console
dotnet-do :completion
```

## Initialize a workspace

Run the initialization wizard in the repository root:

```console
dotnet do :init
```

Accept the defaults to create `dotnetdo.toml`, a `scripts` directory, an initial `build.cs` task, and local launchers.

The generated task is an ordinary .NET file-based app:

```csharp
#!/usr/bin/env dotnet
#:package DotNetDo.Core@0.7.0
using DotNetDo;
using Serilog;

[assembly: TaskDescription("Says hello")]

Log.Information("Hello from {Task}", "build");
```

## Run the task

```console
./do build
```

It logs `Hello from build`. Replace the body with the work the task should do — for example `await Tools.DotNet.Build;`.

Use `dotnet do :help` for runner commands or `dotnet do :help build` for the task's description and declared parameters.

## Next steps

- [Task orchestration](guides/task-orchestration.md) — run tasks in sequence, and group them into meta-tasks in `dotnetdo.toml`.
- [Core API](reference/core/index.md) — what `Do` gives a task: running processes, parameters and secrets, paths and files, the workspace, Git, CI, and logging.
- [Tool API](reference/tools/index.md) — typed commands for the tools a task drives, including the .NET SDK, MSBuild, NuGet, Git, npm, and Bun.
