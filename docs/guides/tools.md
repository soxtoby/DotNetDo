# Tools

DotNetDo models command-line tools as typed C# values. You get discoverable options and consistent command rendering without hiding the command that will run.

## Configure a command

Commands under `Tools` start with defaults suited to the current workspace and build environment. Use a `with` expression to change the options you need:

```csharp
var build = Tools.DotNet.Build with
{
    Configuration = "Release",
    NoRestore = true
};
```

The command is immutable. Another `with` expression creates a changed copy and leaves `build` alone.

## Run or inspect it

Await a command to run it. A non-zero exit code throws.

```csharp
await build;
```

Call `ToString()` when you only need the rendered command line, perhaps for a dry-run task:

```csharp
Log.Information("Would run {Command}", build.ToString());
```

Pass the command to `Do.Exec()` when you need the running process. You can stream its output or choose whether to accept a non-zero exit code:

```csharp
var process = Do.Exec(build);
var result = await process.Completed;

if (result.ExitCode != 0)
    Log.Warning("Build exited with {ExitCode}", result.ExitCode);
```

Execution settings such as the working directory, environment, and output logging are also set with `with` because every tool command carries `ExecOptions`.

See the [Tool API](../reference/tools/index.md) for supported tools and the [execution API reference](../reference/core/execution.yml) for process control.
