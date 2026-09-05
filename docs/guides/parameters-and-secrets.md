# Parameters and secrets

Task parameters keep values that vary between runs out of task code. They can come from command-line arguments, `DOTNETDO_` environment variables, user secrets, `dotnetdo.toml`, or a default declared by the task.

## Declare parameters

Give parameters a type, default, and short description near the top of the task:

```csharp
var configuration = Do.Param(
    "configuration",
    "Release",
    "Build configuration");

var publish = Do.Param<bool>("publish");

await Tools.DotNet.Build with { Configuration = configuration };

if (publish.Value is true)
    Log.Information("Publishing build output");
```

Pass values as long options:

```console
./do build --configuration Debug --publish
```

A Boolean option may omit its value. Other types require one. Call `.Required()` on a parameter without a default when the task cannot continue without it. When running locally in an interactive shell, DotNetDo prompts for any required parameters that are still missing.

Literal `Do.Param` declarations contribute names, descriptions, Boolean values, and same-file enum values to task help and [shell completion](completion.md). DotNetDo discovers them from source without running the task.

## Keep secrets out of logs

Declare credentials with `Do.Secret` rather than `Do.Param`:

```csharp
var token = Do.Secret(
    "deployment-token",
    description: "Token used to publish the release")
    .Required();

await Do.Exec($"deploy --token {token.QuotedArgument()}");
```

DotNetDo masks resolved secrets in its logs and registers them with supported CI providers. Secret input is also masked when an interactive local run prompts for a required value.

Call `Unwrap()` only when an API needs the plain string. Avoid writing that value through APIs outside DotNetDo's redacting logger.

## Store secrets for local use

.NET gives each file-based app its own user-secrets store. Set a value against the task file:

```console
dotnet user-secrets set "deployment-token" "your-secret-value" --file scripts/release.cs
```

DotNetDo reads that value when `release.cs` calls `Do.Secret("deployment-token")`. The secret stays outside the repository. See [.NET user secrets for file-based apps](https://learn.microsoft.com/en-us/dotnet/core/sdk/file-based-apps#user-secrets) for listing and managing stored values.

See the [parameters and secrets API reference](../reference/core/parameters-and-secrets.yml) for resolution order and wrapper types.

## Pass trailing arguments

Use `--` when a task needs to forward arbitrary arguments to another command. `Do.TrailingArguments` returns each value after the separator without treating option-like values as task parameters:

```csharp
var arguments = string.Join(" ", Do.TrailingArguments.Select(argument => argument.QuotedArgument()));
await Do.Exec($"dotnet test {arguments}");
```

```console
./do test -- --filter "Category=Unit"
```

Save this task as `scripts/test.cs`. Here, `Do.TrailingArguments` contains `--filter` and `Category=Unit`, limiting the test run to unit tests. Declared task parameters still go before the separator.
