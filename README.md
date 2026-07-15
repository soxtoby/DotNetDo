# DotNetDo

DotNetDo turns small C# files into repository automation scripts. It is both a global tool and a library, so scripts run with `do` and use typed helpers for processes, paths, Git, .NET, configuration, secrets, logging, and CI providers.

## Install

```console
dotnet tool install --global DotNetDo
```

On .NET 10, run it without installation:

```console
dnx DotNetDo build
```

## Use

```console
do :new build
do
do build
```

A script is an ordinary .NET file-based app:

```csharp
#!/usr/bin/env dotnet
#:package DotNetDo.Core@0.1.0
using DotNetDo;

await Tools.DotNet.Build;
```

Use `do :help` for runner commands or `do :help <name>` for a script's declared parameters.

Licensed under the [MIT License](LICENSE).
