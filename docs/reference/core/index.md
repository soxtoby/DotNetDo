# Core APIs

`Do` is the static facade for everything a task does outside of running an external tool: executing processes, reading parameters, resolving paths, and inspecting the workspace and its repository.

```csharp
await Do.Exec("dotnet --info");
```

Each family below collects its entry points with their complete generated documentation, followed by the supporting types it uses.

- [Execution](execution.yml) — run processes and installed tools, capture output, and control logging and failure behavior.
- [Parameters and secrets](parameters-and-secrets.yml) — declare task parameters and secrets, then resolve them from arguments, environment variables, user secrets, and workspace configuration.
- [Paths and files](paths-and-files.yml) — work with normalized paths, files, archives, and temporary directories.
- [Workspace and solutions](workspace-and-solutions.yml) — find the workspace root, inspect the configured solution, and navigate its projects.
- [Git repositories](git-repositories.yml) — read repository state and verify that operations leave Git-visible content unchanged.
- [CI and logging](ci-and-logging.yml) — detect local builds, read CI metadata, emit provider-native messages, and configure logging.
- [String and collection utilities](string-and-collection-utilities.yml) — the small extension-method set shared by task scripts and command rendering.
