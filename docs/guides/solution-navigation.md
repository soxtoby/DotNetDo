# Solution navigation

DotNetDo can find projects by their logical location in a `.sln` or `.slnx` file. This keeps task code independent of where project files happen to live on disk and gives it access to evaluated MSBuild properties.

## Select the default solution

If the repository root contains one solution, DotNetDo uses it automatically. For repositories with several solutions, set a root-relative path in `dotnetdo.toml`:

```toml
solution-path = "src/Product.slnx"
```

You can also load one in a task:

```csharp
Do.Solution = await Solution.Load("src/Product.slnx");
```

DotNetDo also uses this solution as the default target for .NET tool commands that build, test, restore, format, or otherwise operate on a project or solution.

## Find and inspect a project

Index the solution with the complete solution path. Include solution folders, use `/` separators, and match case.

```csharp
var api = Do.Solution["src/backend/Api"];
var output = api.Directory / "bin" / "Release";
var targetFramework = api.Project.GetPropertyValue("TargetFramework");

Log.Information("{Project} targets {Framework}", api.Name, targetFramework);
```

Use `api.Path` for the project file and `api.Directory` for its containing directory. `api.Project` evaluates the project with the current MSBuild environment. If you need another configuration, load a separate evaluation with global properties.

See the [workspace and solutions API reference](../reference/core/workspace-and-solutions.yml) for the complete model.
