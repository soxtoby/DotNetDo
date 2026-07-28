# Shell completion

Install user-scoped completion for the detected shell:

```text
dotnet-do :completion
```

Restart the shell afterward. PowerShell is selected on Windows; Bash or Zsh is selected from `$SHELL` elsewhere. Override detection by naming `pwsh`, `bash`, or `zsh`.

Completion is registered for `dotnet-do`; it does not replace the .NET SDK's `dotnet` completer or claim shell-reserved and path-qualified launcher forms. It completes DotNetDo commands, task names, declared task parameters, boolean values, and members of enums declared in the same task file. Enum discovery expects conventional formatting with the declaration braces and each member on separate lines. Meta-tasks combine parameters from all recursively invoked C# tasks. Conflicting parameter metadata keeps the parameter name but omits its detail and values.

Completion only reads workspace configuration and task source. It never executes or compiles tasks, restores packages, or contacts the network.

Remove completion from the detected shell:

```text
dotnet-do :completion uninstall
```

An optional shell name may follow `uninstall`.
