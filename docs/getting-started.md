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

await Tools.DotNet.Build;
```

## Run the task

```console
./do build
```

Use `dotnet do :help` for runner commands or `dotnet do :help build` for the task's declared parameters.

Next, read [Task orchestration](guides/task-orchestration.md) or choose an [API family](reference/index.md).
