# Workspace initialization

`dotnet do :init` is a wizard that initializes the current directory as a DotNetDo workspace. It has no non-interactive argument form.

## Flow

1. If `dotnetdo.toml` exists, load its scripts and solution settings. Otherwise, if an ancestor contains configuration, show its path and ask whether to create a nested workspace. Declining or cancelling exits nonzero without writing.
2. Prompt for a missing scripts path, defaulting to `scripts`. Accept any valid root-relative scripts path, including `.`, unless it names an existing file.
3. When the scripts path contains no direct `.cs` files, prompt for the extensionless initial task name, defaulting to `build`. Task-name validation matches `:new`.
4. Find solutions with `GlobFiles(["**/*.sln", "**/*.slnx"])`. Order them by relative-path depth, then alphabetically. Select the only result automatically; require an explicit numbered choice when several exist.
5. When a solution is selected, ask whether to add the scripts to it; yes is the default.
6. Update configuration first, then create a missing scripts directory and initial task, synchronize accepted solution integration, and create either missing root-local launcher.

Initialization is resumable. Re-entering an existing workspace fills missing setup: it may create the scripts directory and initial task when no direct task files exist, discover and persist a solution path, synchronize the solution folder, and create missing launchers. Existing task and launcher files are preserved.

When a solution is configured, initialization asks `Add scripts to '<solution>'? [Y/n]:`. Yes is the default. On acceptance, it uses the configured `solution-folder`, adding `solution-folder = "scripts"` when absent. It does not prompt for the folder name or remove a differently named solution folder.

Empty prompt input accepts a default. Invalid input explains the error and prompts again. EOF or cancellation exits nonzero without writing.

## Configuration

Paths are stored root-relative with `/` separators. `scripts-path` is always written; `solution-path` is written when a solution was found or selected.

```toml
scripts-path = "scripts"
solution-path = "Product.slnx"
solution-folder = "scripts"
```

A configured `solution-path` is authoritative. It must be a root-contained relative path naming an existing `.sln` or `.slnx` file; DotNetDo does not fall back to discovery when it is invalid.

`solution-folder` is the simple name of the root solution folder containing the task scripts. It must be non-empty and cannot contain `/` or `\\`.

Solution integration includes `.cs` files recursively below `scripts-path`. This is intentionally broader than runnable-task discovery, which remains limited to direct `.cs` children.

Within the configured root solution folder, initialization owns only `.cs` file entries. For `.sln`, it replaces those entries with the current recursive filesystem set. For `.slnx`, it replaces them with one recursive glob. Non-C# files, child folders, and projects are preserved.

Solution item paths are relative to the solution directory, not the workspace root, and use `/` separators. A solution below the workspace may therefore use a glob such as `../scripts/**/*.cs`; a root scripts path uses `**/*.cs`.

## Failure and output

Initialization is not transactional. It writes configuration before applying filesystem and solution changes; a later failure leaves earlier successful changes for the next `:init` run to continue.

The generated `do.cmd` runs `dnx DotNetDo %*`. The generated executable `do` script runs `exec dnx DotNetDo "$@"`. Invoke them by path: `.\do <task-name>` in PowerShell or `./do <task-name>` in a Unix shell.

On success, report updated configuration and each created directory, task, launcher, or solution integration. Do not run the task.
