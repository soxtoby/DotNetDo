# Path values

## Construction

`AbsolutePath` and `RelativePath` are sealed records constructed explicitly from strings. There is no implicit conversion from `string`.

Internally, a path is a typed root where applicable plus an immutable collection of already-parsed segments. Operations use this structure and never reparse rendered path text.

An absolute path is one of:

- Unix-rooted: `/a`
- Windows drive-rooted: `C:\a`
- UNC-rooted: `\\server\share\a` or `//server/share/a`

A UNC root requires non-empty server and share components. The share is the traversal boundary.

Drive-relative (`C:a`) Windows paths are rejected by both types: they are contextual paths, not ordinary relative paths. Root-relative (`\a`) Windows paths are not absolute and are likewise rejected. A relative path has no root. Both `/` and `\` are accepted as input separators. Only NUL and malformed roots are otherwise rejected; platform-specific filename restrictions are not enforced.

Paths are lexical: construction never requires existence, accesses the filesystem, resolves symlinks, expands `~`, expands environment variables, or resolves against the current directory.

Filesystem operations are exposed only by `AbsolutePath`. `RelativePath` never resolves implicitly against the process working directory; callers must join it to an absolute base first.

These operations are instance members of `AbsolutePath`; no separate filesystem service owns them. This does not change lexical construction, normalization, or identity.

`AbsolutePath` exposes live, uncached filesystem classification properties:

```csharp
bool Exists { get; }
bool IsExistingFile { get; }
bool IsExistingDirectory { get; }
```

These properties directly use `File.Exists` and `Directory.Exists`, including their native handling of missing paths, links, invalid paths, and access failures. `Exists` is `IsExistingFile || IsExistingDirectory`.

`AbsolutePath.EnsureDirectoryExists()` recursively creates missing directories, does nothing when the path already identifies a directory, and returns the same path value for chaining. It throws when the path identifies a file or creation otherwise fails.

`EnsureDirectoryExists()` directly uses `Directory.CreateDirectory`, including its native link and error behavior.

`AbsolutePath` exposes synchronous, typed file-content helpers:

```csharp
string ReadText(Encoding? encoding = null);
string[] ReadLines(Encoding? encoding = null);
void WriteText(string text, Encoding? encoding = null);
void WriteLines(IEnumerable<string> lines, Encoding? encoding = null);

T? ReadJson<T>(JsonSerializerOptions? options = null);
void WriteJson<T>(T value, JsonSerializerOptions? options = null);
T? ReadToml<T>(TomlSerializerOptions? options = null);
void WriteToml<T>(T value, TomlSerializerOptions? options = null);
T? ReadYaml<T>(IDeserializer? deserializer = null);
void WriteYaml<T>(T value, ISerializer? serializer = null);
T? ReadXml<T>();
void WriteXml<T>(T value);
```

Text helpers delegate to the corresponding eager `File` operations. A null encoding uses the native UTF-8 default. Structured helpers use `System.Text.Json`, Tomlyn, YamlDotNet, and `XmlSerializer` respectively. JSON and TOML expose their native options objects. YAML accepts native YamlDotNet `IDeserializer` and `ISerializer` instances; null uses cached plain builder-created instances with no DotNetDo naming, converter, or tolerance policy. Supplied instances remain caller-owned. YAML uses UTF-8, exposes no encoding parameter, and reads or writes one typed document. Reads retain the `T?` shape and writes pass nullable values through to YamlDotNet. Output is not normalized after serialization. XML initially uses serializer defaults.

Reads preserve serializer nullability and propagate native missing-file, malformed-content, and type errors without DotNetDo exception wrapping. Writes create or overwrite the file directly, return no value, and propagate native errors. They do not create missing parent directories, validate filename extensions, append, write atomically, create backups, or add formatting policy. Structured output uses each serializer's defaults.

`AbsolutePath` also owns uniform copy, move, and delete operations for files and directories. Missing paths and symbolic links follow the underlying `System.IO` behavior. The same method names apply to both filesystem kinds rather than exposing parallel file and directory families.

```csharp
void Delete();
```

`Delete()` removes files directly and directories recursively, following the underlying `System.IO` behavior for missing paths and symbolic links.

Delete and overwrite respect native attributes and permissions. They propagate read-only and access failures rather than clearing protections automatically.

Copy and move distinguish exact destinations from destination containers:

```csharp
AbsolutePath CopyTo(AbsolutePath destination, TransferOptions? options = null);
AbsolutePath CopyInto(AbsolutePath directory, TransferOptions? options = null);
AbsolutePath MoveTo(AbsolutePath destination, TransferOptions? options = null);
AbsolutePath MoveInto(AbsolutePath directory, TransferOptions? options = null);
```

`To` uses the exact destination path. `Into` uses a destination directory, preserves the source name beneath it, and returns that final child path. The directory must exist unless `CreateDirectories` is enabled. Both forms return the final destination path.

Copy and move fail by default when the final destination exists. Their options can enable two independent behaviors:

- `CreateDirectories` makes `To` create missing destination parents and makes `Into` create its destination container.
- `Overwrite` allows a file to replace a file, or a directory to merge into a directory while recursively replacing colliding files.

Even with `Overwrite`, a file/directory type conflict throws; transfer operations never recursively delete an entry merely to change its filesystem kind.

Symbolic links follow the behavior of the underlying `System.IO` operations; DotNetDo adds no link-specific policy.

Moving a directory beneath itself retains `Directory.Move` failure behavior; it cannot use copy/delete fallback because deleting the source would delete the destination.

Move uses a native rename when possible. When a move cannot cross filesystems, it falls back to copy then delete with the same transfer options. A copy failure leaves the source in place; if deletion fails after copying, both locations may remain and the failure propagates. Cross-filesystem move is not atomic.

Recursive transfers are not transactional. If a transfer fails, destination changes already completed remain. A copy-based move does not begin deleting its source until the complete copy phase succeeds.

```csharp
public sealed record TransferOptions
{
    public bool Overwrite { get; init; }
    public bool CreateDirectories { get; init; }
}
```

A **search root** is the absolute directory that explicitly bounds a glob search. It is the receiver for type-specific glob operations:

```csharp
root.GlobFiles(patterns, options);
root.GlobDirectories(patterns, options);
```

Both return absolute path values. There is no mixed-entry `Glob` operation.

Glob results contain descendants of the search root only; the search root itself is never a match candidate.

Glob membership has set semantics. Overlapping inclusions yield one result per path; exclusions and later re-inclusions change membership without creating duplicates.

Globbing uses `Microsoft.Extensions.FileSystemGlobbing` pattern and error semantics. Patterns are evaluated in order. A leading `!` makes a pattern an exclusion; `\!` escapes a literal leading `!`. A later inclusion can re-include a prior exclusion. Pattern validation is owned by the Microsoft matcher.

Pattern matching uses the Microsoft matcher's case-insensitive default unless overridden through `GlobOptions`.

```csharp
public sealed record GlobOptions
{
    public StringComparison Comparison { get; init; } = StringComparison.OrdinalIgnoreCase;
}
```

`GlobFiles` delegates filesystem traversal and matching to `Matcher.GetResultsInFullPath`. `GlobDirectories` uses native recursive directory enumeration, then applies the same matcher to the relative directory paths because the Microsoft API returns file matches only.

Glob result order is unspecified and follows the underlying matcher and filesystem enumeration.

Glob searches execute eagerly. Each method returns a completed snapshot and reports search errors during the call.

Constructors throw `ArgumentException` for the wrong path kind, malformed roots, NUL, or traversal above an absolute root.

## Normalization and identity

Dot segments normalize during construction and joining:

- `a/./b` becomes `a/b`.
- `a/../b` becomes `b`.
- `../a` remains `../a`.
- `/x/../y` becomes `/y`.
- Traversal above an absolute root is invalid.

Repeated and trailing separators normalize away, except the separator required by a root. `//server/share` is UNC; a Unix root uses one leading slash.

An empty relative path exists as `RelativePath.Empty` and is the join identity. It renders as `.` and has no `Name` or `Parent`.

`RelativePath.Raw(string segment)` creates one opaque segment without separator parsing. It rejects empty text, `.`, `..`, and NUL. Separator characters inside any other value remain literal segment content; formatting never rewrites them. `AbsolutePath` has no raw factory because its root must be parsed, but an absolute path can join a raw relative segment.

Equality and hashing use the structural root and segment text with ordinal, case-sensitive comparison on every OS. Rendering style and construction history do not affect identity. Thus `Raw("a")` equals parsed `a`, while raw `a\b` is one segment and parsed `a\b` is two.

## Joining

The `/` operator supports:

```csharp
AbsolutePath / RelativePath // AbsolutePath
AbsolutePath / string       // AbsolutePath
RelativePath / RelativePath // RelativePath
RelativePath / string       // RelativePath
```

The right operand must be relative. Absolute-to-absolute and left-hand string operators do not exist. A rooted or otherwise invalid string right operand throws `ArgumentException`. Joining a valid relative path that escapes an absolute root throws `InvalidOperationException`.

## Rendering

Both types expose:

```csharp
string UnixPath { get; }
string WindowsPath { get; }
```

Format properties change separators only; they never map drive or share roots into another root model. `ToString()`, and the implicit conversion from a path value to `string`, use the current OS separator style.

Both types expose `QuotedArgument()` for raw command composition. It renders using the current OS separator style, then applies the same conditional command-line quoting as `string.QuotedArgument()`.

## Metadata

Both types expose:

```csharp
string? Name { get; }
string Extension { get; }
string? NameWithoutExtension { get; }
```

`Name` is the final segment, regardless of whether the filesystem would treat it as a file or directory. Roots and the empty relative path have no name. Extension behavior matches `System.IO.Path`: `a.txt` has `.txt`, `archive.tar.gz` has `.gz`, while `.gitignore` and `file.` have no extension.

`AbsolutePath.Parent` is `AbsolutePath?`; roots have no parent. `RelativePath.Parent` is `RelativePath?`; a single-segment or empty relative path has no parent. For example, `a/b` has parent `a`, and `../a` has parent `..`.

`AbsolutePath` additionally exposes:

```csharp
AbsolutePath Root { get; }
bool IsRoot { get; }
```

`Root` preserves its root model: `/a/b` returns `/`, `C:\a` returns `C:\`, and `\\server\share\a` returns `\\server\share\`. No public root-kind, drive, server, or share API is initially provided.
