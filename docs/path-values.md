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
string NativePath { get; }
```

Format properties change separators only; they never map drive or share roots into another root model. `ToString()`, and the implicit conversion from a path value to `string`, return `NativePath` using the current OS separator style.

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
