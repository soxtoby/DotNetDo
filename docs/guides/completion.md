# Shell completion

Shell completion suggests DotNetDo commands, task names, and task parameters as you type. It saves trips to task help and makes repository tasks easier to discover.

## Install completion

Run this once, then restart your shell:

```console
dotnetdo :completion
```

DotNetDo detects PowerShell on Windows and Bash or Zsh elsewhere. To choose a shell explicitly, add `pwsh`, `bash`, or `zsh`:

```console
dotnetdo :completion zsh
```

Completion works with `dotnetdo` and with a workspace launcher such as `./do`. It suggests parameters declared with `Do.Param`, including Boolean and enum values. Meta-tasks include parameters from the C# tasks they run.

The installed `dotnetdo` command serves completion for workspace launchers. Installation fails without changing your shell profile when that command is not on `PATH`; uninstall remains available if the command is later removed. Pressing Tab after `./do` does not invoke `dnx` or resolve the workspace's DotNetDo package.

Completion reads configuration and task source without executing your tasks or restoring their packages.

## Uninstall completion

```console
dotnetdo :completion uninstall
```

Add a shell name after `uninstall` if you do not want DotNetDo to detect it.
