# NuGet CLI

`Tools.NuGet` provides typed immutable commands for the host-owned `nuget` executable. Commands return raw `ExecResult` output and can be customized with record `with` expressions.

```csharp
await Tools.NuGet.EnsureAvailable;

await Tools.NuGet.Pack with
{
    InputPath = Do.RootDirectory / "package.nuspec",
    OutputDirectory = Do.RootDirectory / "artifacts",
};
```

The suite includes `Add`, `Config`, `Delete`, `Init`, `Install`, `List`, `Locals`, `Pack`, `Push`, `Restore`, `Search`, `SetApiKey`, `Sign`, `Sources`, `Spec`, `TrustedSigners`, `Update`, and `Verify`. It intentionally omits help commands and executable self-update.

The executable and version are owned by the host. `EnsureAvailable` uses an existing `nuget` from `PATH`, or installs NuGet through Scoop on Windows. Existing Mono-based installations may work on other platforms, but DotNetDo does not install them or promise cross-platform compatibility. Commands never install implicitly.

Use `Secret` for API keys and passwords:

```csharp
await Tools.NuGet.Push with
{
    Package = "artifacts/*.nupkg",
    Source = "https://api.nuget.org/v3/index.json",
    ApiKey = new Secret(apiKey),
};
```

Typed properties follow the current official `nuget.exe` reference. DotNetDo validates only inputs needed to render a command; NuGet remains responsible for conflicting, dependent, version-specific, and mode-specific options. `AdditionalArguments` is appended last and remains available for unmodeled future syntax.
