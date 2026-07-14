# Workspace initialization

`do :init` is a wizard that initializes the current directory as a DotNetDo workspace. It has no non-interactive argument form.

## Flow

1. If `dotnetdo.toml` exists in the current directory, fail without prompting.
2. If an ancestor contains `dotnetdo.toml`, show its path and ask whether to create a nested workspace. No is the default and exits successfully without writing.
3. Prompt for the scripts path, defaulting to `scripts`. Accept any valid root-relative scripts path, including `.`.
4. Prompt for the extensionless initial app name, defaulting to `build`. App-name validation matches `:new`.
5. Find solutions with `GlobFiles(["**/*.sln", "**/*.slnx"])`. Order them by relative-path depth, then alphabetically. Select the only result automatically; require an explicit numbered choice when several exist.
6. Preflight all targets before writing. A pre-existing scripts directory is reused; a pre-existing initial script fails initialization.
7. Create the scripts directory if needed, create the initial app using the same template and executable handling as `:new`, then write `dotnetdo.toml` last.

Empty prompt input accepts a default. Invalid input explains the error and prompts again. EOF or cancellation exits nonzero without writing.

## Configuration

Paths are stored root-relative with `/` separators. `scripts-path` is always written; `solution-path` is written when a solution was found or selected.

```toml
scripts-path = "scripts"
solution-path = "Product.slnx"
```

A configured `solution-path` is authoritative. It must be a root-contained relative path naming an existing `.sln` or `.slnx` file; DotNetDo does not fall back to discovery when it is invalid.

## Failure and output

Initialization performs best-effort rollback of only the artifacts it created. It never removes a pre-existing scripts directory. Writing configuration last ensures the workspace marker represents completed initialization.

On success, report the created configuration, directory and script, any selected solution, and `Run with: do <app-name>`. Do not run the app.
