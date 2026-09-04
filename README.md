# DotNetDo

DotNetDo turns small C# files into repository automation scripts. Install the `dotnetdo` global tool to initialize a workspace, then run its scripts through the local `./do` launcher. Scripts use typed helpers for processes, paths, Git, .NET, configuration, secrets, logging, and CI providers.

## Getting started

Note that DotNetDo requires .NET 10.

```console
dotnet tool install --global DotNetDo
dotnetdo :completion
```

The second command installs task and parameter completion for PowerShell, Bash, or Zsh. Restart the shell afterward.

```console
dotnetdo :init
./do :new build
./do build
```

`:init` creates workspace-local `do.cmd` and `do` launchers. Use `./do` from your shell.

A script is an ordinary .NET file-based app:

```csharp
#!/usr/bin/env dotnet
#:package DotNetDo.Core@0.7.0
using DotNetDo;

await Tools.DotNet.Build;
```

Use `./do :help` for runner commands or `./do :help <name>` for a script's declared parameters.

Read the [documentation](https://soxtoby.github.io/DotNetDo/), browse the [API reference](https://soxtoby.github.io/DotNetDo/reference/), or continue with the [guides](https://soxtoby.github.io/DotNetDo/guides/task-orchestration.html).

Licensed under the [MIT License](LICENSE).
