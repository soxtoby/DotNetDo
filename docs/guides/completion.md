# Shell completion

Shell completion suggests DotNetDo commands, task names, and task parameters as you type. It saves trips to task help and makes repository tasks easier to discover.

## Install completion

Run this once, then restart your shell:

```console
dotnet-do :completion
```

DotNetDo detects PowerShell on Windows and Bash or Zsh elsewhere. To choose a shell explicitly, add `pwsh`, `bash`, or `zsh`:

```console
dotnet-do :completion zsh
```

Completion works with `dotnet-do` and with a workspace launcher such as `./do`. It suggests parameters declared with `Do.Param`, including Boolean and enum values. Meta-tasks include parameters from the C# tasks they run.

Completion reads configuration and task source without executing your tasks or restoring their packages.

## Uninstall completion

```console
dotnet-do :completion uninstall
```

Add a shell name after `uninstall` if you do not want DotNetDo to detect it.
