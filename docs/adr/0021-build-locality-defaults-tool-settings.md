# Build locality defaults tool settings

DotNetDo exposes process-stable `Do.IsLocalBuild`, which is false when GitHub Actions, Azure Pipelines, or the conventional `CI` marker is active. Fresh typed tool commands may map non-local builds to family-specific sensible defaults: .NET commands use `Release` configuration and MSBuild seeds its `Configuration` property, while local builds preserve native or project defaults. Each tool family owns its mapping rather than sharing a universal settings registry, because equivalent CI intent uses different native vocabulary across ecosystems.
