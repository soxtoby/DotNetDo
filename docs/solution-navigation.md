# Solution navigation

DotNetDo exposes a small, read-only model for finding projects by their logical location in a `.sln` or `.slnx` file and obtaining their physical paths and evaluated MSBuild data.

## Authored shape

```csharp
var api = Do.Solution["src/backend/Api"];

AbsolutePath solutionFile = Do.Solution.Path;
AbsolutePath solutionDirectory = Do.Solution.Directory;
string projectName = api.Name;
AbsolutePath projectFile = api.Path;
AbsolutePath projectDirectory = api.Directory;

var targetFramework = api.Project.GetPropertyValue("TargetFramework");
```

An explicit solution is constructed separately:

```csharp
var solution = await Solution.Load("src/Product.slnx");
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
    public static Task<Solution> Load(string path, CancellationToken cancellationToken = default);
    public static Task<Solution> Load(AbsolutePath path, CancellationToken cancellationToken = default);

    public AbsolutePath Path { get; }
    public AbsolutePath Directory { get; }
    public IReadOnlyList<ProjectInfo> Projects { get; }
    public ProjectInfo this[string solutionPath] { get; }
}

public sealed class ProjectInfo
{
    public string Name { get; }
    public string SolutionPath { get; }
    public AbsolutePath Path { get; }
    public AbsolutePath Directory { get; }
    public Microsoft.Build.Evaluation.Project Project { get; }

    public Microsoft.Build.Evaluation.Project Load(
        IReadOnlyDictionary<string, string> globalProperties);
}
```

The concrete dictionary parameter may use the closest shape required by the Microsoft API, but callers must be able to supply arbitrary global MSBuild properties.

## Default solution

`Do.Solution` is lazily initialized once per process. When `solution-path` is configured, its root-relative `.sln` or `.slnx` file is authoritative and invalid values fail without discovery fallback. Otherwise the DotNetDo root must contain exactly one `.sln` or `.slnx` file. Discovery does not search subdirectories or ancestors. No match or multiple matches fail with a useful error.

The property is assignable so a script or test can replace the process default before or after discovery. Assigning `null` fails; there is no reset-to-discovery behavior.

`Solution.Load(string)` resolves a relative path against the current directory. Both factory overloads require an existing `.sln` or `.slnx` file and reject directories and other extensions. Only the lazy `Do.Solution` getter blocks while loading its discovered default.

## Project identity and lookup

A solution path joins solution-folder names and the project name with `/`. A root project uses only its name. It has no leading slash and is a virtual `string`, not a filesystem `RelativePath`.

`Name` is the project name authored in the solution. It is the final segment of `SolutionPath` and may differ from the project filename or assembly name. Names are not unique; lookup continues to require the complete solution path.

Lookup is ordinal case-sensitive and requires the complete solution path. It never falls back to a unique leaf name. A missing lookup throws an error that includes the requested value and available solution paths.

`Projects` is flat and contains every file-backed project entry, including non-.NET projects. Solution items and solution folders are not projects and have no public model in v1. A stale entry whose project file is missing remains navigable; evaluation fails when requested.

## Parsing and paths

Use Microsoft's `Microsoft.VisualStudio.SolutionPersistence` package for both `.sln` and `.slnx`; DotNetDo does not own either file-format parser.

Solution and project `Path` values are absolute `AbsolutePath` values. `Directory` is their parent directory. Project paths are resolved relative to the solution directory according to the parsed solution model.

The loaded solution model is immutable and cached by the `Solution` instance. It does not watch or reload the solution file.

## MSBuild evaluation

Use `Microsoft.Build.Locator` to load the active .NET SDK's Microsoft MSBuild object model. This supports SDK-style projects and ordinary old-style .NET Framework projects from a .NET 10 task. Projects requiring unavailable imports, targeting packs, workloads, or Visual Studio-only toolsets fail with their native MSBuild error, augmented with project context. DotNetDo does not fall back to partial XML interpretation.

`Project` lazily evaluates the project with the MSBuild global project collection's current global properties. `ProjectInfo` caches the first successful evaluation, returns that same mutable `Project` for its lifetime, and retries after a failed evaluation. The cache is thread-safe.

`Load(globalProperties)` evaluates the project with the supplied global properties. MSBuild's global project collection owns every evaluation for the process lifetime and returns an existing mutable `Project` when the project path and global properties match. DotNetDo exposes no unload or reset API; directly unloading a cached project through MSBuild is unsupported.

DotNetDo ships a source generator that injects MSBuild Locator registration into the consuming app's module initializer. Registration therefore happens before that module's methods can resolve the public Microsoft MSBuild types.

The default `Project` evaluation inherits the global collection's properties. `Load(globalProperties)` forwards caller-provided values such as `Configuration`, `Platform`, and `TargetFramework` as the evaluation's global properties.

Evaluation reads project state; this API does not expose build execution.
