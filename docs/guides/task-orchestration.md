# Task orchestration

Meta-tasks group existing tasks into named workflows. Use them when the same sequence runs locally and in CI, or when you want a convenient command for part of a larger build.

## Define a workflow

Add a `tasks` table to `dotnetdo.toml`. A string runs one task. An array runs tasks in order.

```toml
[tasks]
test = ["build", "test-components"]
test-components = ["test-csharp", "test-javascript"]
coverage = "test-csharp --coverage"
```

You can now choose the workflow you need:

```console
./do test
./do test-components
./do coverage
```

For example, CI can run `build` in one step and `test-components` in another without rebuilding.

## Pass arguments

Arguments supplied to a meta-task go to every task it runs. Fixed arguments in `dotnetdo.toml` come afterward and take precedence for DotNetDo parameters.

```toml
[tasks]
release-test = [
  "build --configuration Release",
  "test-csharp --configuration Release"
]
```

`./do release-test` now keeps both tasks on the same configuration.

Meta-tasks run sequentially and stop at the first failure. They may call other meta-tasks, but cannot contain cycles. Use C# task code when you need conditions, parallel work, or cleanup.

Run `./do :help release-test` to inspect a meta-task.

## Share code between tasks

Keep shared source files in a subfolder of the scripts directory. DotNetDo treats only `.cs` files directly in the scripts directory as tasks, so the shared file will not appear as another runnable task.

Include the file at the top of each task that needs it:

```csharp
#:include shared/BuildSettings.cs
```

Included C# files can declare types and methods, but cannot contain top-level statements. `#:include` requires .NET SDK 10.0.300 or later.

For a larger shared library, reference its project instead:

```csharp
#:project ../build/BuildTasks.csproj
```

Both paths are relative to the task file. Use a project when the shared code needs its own dependencies, build settings, or tests.
