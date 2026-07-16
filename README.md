# DotNetDo

DotNetDo turns small C# files into repository automation scripts. It is both a global tool and a library, so scripts run with `dotnet do` and use typed helpers for processes, paths, Git, .NET, configuration, secrets, logging, and CI providers.

## Install

Note that DotNetDo requires .NET 10.

```console
dotnet tool install --global DotNetDo
```

```console
dnx DotNetDo build
```

## Use

```console
dotnet do :new build
dotnet do
dotnet do build
```

`:init` creates workspace-local `do.cmd` and `do` launchers. Use `.\do` from PowerShell or `./do` from a Unix shell.

A script is an ordinary .NET file-based app:

```csharp
#!/usr/bin/env dotnet
#:package DotNetDo.Core@0.1.0
using DotNetDo;

await Tools.DotNet.Build;
```

Use `dotnet do :help` for runner commands or `dotnet do :help <name>` for a script's declared parameters.

Licensed under the [MIT License](LICENSE).
