# Path values

`AbsolutePath` and `RelativePath` make the difference between filesystem locations and repository-relative names explicit. They normalize separators and `.` segments, work on Windows and Unix, and prevent a relative path from silently escaping an absolute root.

## Build paths

Use `Do.RootDirectory` as the base for repository files, then join path segments with `/`:

```csharp
AbsolutePath artifacts = Do.RootDirectory / "artifacts";
AbsolutePath package = artifacts / "packages" / "Widget.1.0.0.nupkg";
RelativePath changelog = new("CHANGELOG.md");

artifacts.EnsureDirectoryExists();
```

Construct `AbsolutePath` when you already have a rooted path and `RelativePath` for a path that needs a base. Construction is lexical, so the path does not need to exist.

## Read and write files

Path values include common text and structured-data operations. For example, a task can update a JSON manifest without switching between path strings and filesystem helpers:

```csharp
var manifestPath = Do.RootDirectory / "manifest.json";
var manifest = manifestPath.ReadJson<Manifest>() ?? new Manifest();

manifest.Version = "1.0.0";
manifestPath.WriteJson(manifest);
```

JSON, TOML, YAML, and XML can be read into typed values or their native document models. Writes overwrite the file and expect its parent directory to exist.

## Find and move files

Glob from an explicit search root. File and directory searches are separate:

```csharp
var packages = artifacts.GlobFiles(["packages/**/*.nupkg"]);
var publish = (artifacts / "publish").EnsureDirectoryExists();

foreach (var packagePath in packages)
    packagePath.CopyInto(publish, new() { Overwrite = true });
```

`CopyTo` and `MoveTo` take an exact destination. `CopyInto` and `MoveInto` preserve the source name inside a destination directory. Path values also support deletion, temporary files and directories, and ZIP archives.

Use `QuotedArgument()` when inserting a path into a raw command string. Typed tool commands quote their structured path arguments for you.

See the [paths and files API reference](../reference/core/paths-and-files.yml) for all operations and options.
