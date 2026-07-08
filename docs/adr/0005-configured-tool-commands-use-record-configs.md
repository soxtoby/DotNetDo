# Configured tool commands use record configs

Configured tool commands use immutable record configs with public `init` properties instead of mutable fluent builders. Properties own their argument-slot writes in their accessors, so authored config stays data-shaped while rendering remains slot-based; shared dotnet option groups remain public non-generic base records because the CLI shares those options across commands. Command entrypoints accept config values directly, with bare overloads constructing default configs internally and no transform-callback overloads.
