# Solution navigation

DotNetDo exposes a small, read-only model for finding projects by their logical location in a `.sln` or `.slnx` file and obtaining their physical paths and evaluated MSBuild data.

## Authored shape

```csharp
var api = Do.Solution["src/backend/Api"];

AbsolutePath solutionFile = Do.Solution.Path;
AbsolutePath solutionDirectory = Do.Solution.Directory;
AbsolutePath projectFile = api.Path;
AbsolutePath projectDirectory = api.Directory;

using var loaded = api.Load();
var targetFramework = loaded.Project.GetPropertyValue("TargetFramework");
```

An explicit solution is constructed separately:

```csharp
var solution = await Solution.LoadAsync("src/Product.slnx");
Do.Solution = solution;
```

## Public model

```csharp
public static partial class Do
{
    public static Solution Solution { get; set; }
}

public sealed class Solution
{
    public static Task<Solution> LoadAsync(string path, CancellationToken cancellationToken = default);
    public static Task<Solution> LoadAsync(AbsolutePath path, CancellationToken cancellationToken = default);

    public AbsolutePath Path { get; }
    public AbsolutePath Directory { get; }
    public IReadOnlyList<ProjectInfo> Projects { get; }
    public ProjectInfo this[string solutionPath] { get; }
}

public sealed class ProjectInfo
{
    public string SolutionPath { get; }
    public AbsolutePath Path { get; }
    public AbsolutePath Directory { get; }

    public LoadedProject Load(
        IReadOnlyDictionary<string, string>? globalProperties = null);
}

public sealed class LoadedProject : IDisposable
{
    public Microsoft.Build.Evaluation.Project Project { get; }
    public void Dispose();
}
```

The concrete dictionary parameter may use the closest shape required by the Microsoft API, but callers must be able to supply arbitrary global MSBuild properties.

## Default solution

`Do.Solution` is lazily initialized once per process. When `solution-path` is configured, its root-relative `.sln` or `.slnx` file is authoritative and invalid values fail without discovery fallback. Otherwise discovery starts at the DotNetDo root and walks upward; the first directory containing solution files wins and must contain exactly one. No match or multiple matches fail with a useful error.

The property is assignable so a script or test can replace the process default before or after discovery. Assigning `null` fails; there is no reset-to-discovery behavior.

`Solution.LoadAsync(string)` resolves a relative path against the current directory. Both factory overloads require an existing `.sln` or `.slnx` file and reject directories and other extensions. Only the lazy `Do.Solution` getter blocks while loading its discovered default.

## Project identity and lookup

A solution path joins solution-folder names and the project name with `/`. A root project uses only its name. It has no leading slash and is a virtual `string`, not a filesystem `RelativePath`.

Lookup is ordinal case-sensitive and requires the complete solution path. It never falls back to a unique leaf name. A missing lookup throws an error that includes the requested value and available solution paths.

`Projects` is flat and contains every file-backed project entry, including non-.NET projects. Solution items and solution folders are not projects and have no public model in v1. A stale entry whose project file is missing remains navigable; evaluation fails when requested.

## Parsing and paths

Use Microsoft's `Microsoft.VisualStudio.SolutionPersistence` package for both `.sln` and `.slnx`; DotNetDo does not own either file-format parser.

Solution and project `Path` values are absolute `AbsolutePath` values. `Directory` is their parent directory. Project paths are resolved relative to the solution directory according to the parsed solution model.

The loaded solution model is immutable and cached by the `Solution` instance. It does not watch or reload the solution file.

## MSBuild evaluation

Use `Microsoft.Build.Locator` to load the active .NET SDK's Microsoft MSBuild object model. This supports SDK-style projects and ordinary old-style .NET Framework projects from a .NET 10 task. Projects requiring unavailable imports, targeting packs, workloads, or Visual Studio-only toolsets fail with their native MSBuild error, augmented with project context. DotNetDo does not fall back to partial XML interpretation.

`Load()` returns an owning `LoadedProject` containing the raw, mutable Microsoft `Project`. Each call freshly loads and evaluates the project and its imports. Disposing the wrapper unloads the project and disposes its `ProjectCollection`.

DotNetDo ships a source generator that injects MSBuild Locator registration into the consuming app's module initializer. Registration therefore happens before that module's methods can resolve the public Microsoft MSBuild types.

Parameterless evaluation supplies no DotNetDo-defined global properties and preserves MSBuild's defaults. The overload forwards caller-provided values such as `Configuration`, `Platform`, and `TargetFramework` as global properties.

Evaluation reads project state; this API does not expose build execution.
