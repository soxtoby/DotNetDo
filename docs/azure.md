# Azure CLI

Declare `azure` in the workspace tool requirements, then run `dotnet do :install` to make the `az` command available:

```toml
tools = ["azure"]
```

`Tools.Azure.Bicep` provides typed commands for Bicep authoring, modules, deployment snapshots, installation, and upgrades:

```csharp
using System.Text.Json;

var template = Do.RootDirectory / "infra" / "main.bicep";

await Tools.Azure.Bicep.Lint with { File = template };

var build = await Tools.Azure.Bicep.Build with
{
    File = template,
    Stdout = true,
};

var armTemplate = build.ReadJson<JsonDocument>();
```

The supported commands are `Build`, `BuildParams`, `Lint`, `Format`, `GenerateParams`, `Restore`, `Publish`, `Snapshot`, `Install`, and `Upgrade`. Awaiting them returns `ExecResult`; use its `ReadText()` or `ReadJson<T>()` readers for standard-output content.

`Tools.Azure.Install` installs Azure CLI. `Tools.Azure.Bicep.Install` runs `az bicep install` for explicit Bicep version or platform control. Other `az bicep` commands retain Azure CLI's native behavior of acquiring its internally managed Bicep binary when needed.
