namespace DotNetDo;

/// <summary>An immutable, normalized path relative to an unspecified base.</summary>
public sealed record RelativePath
{
    internal RelativePath(string[] segments) => Segments = segments;

    internal string[] Segments { get; }

    /// <summary>The path containing no components, rendered as <c>.</c>.</summary>
    public static RelativePath Empty { get; } = new([]);

    /// <summary>Normalizes relative path text without accessing the filesystem.</summary>
    /// <param name="path">Relative path text; may use either separator. Rooted, drive-relative, and NUL-containing paths are rejected. Empty and <c>.</c> produce <see cref="Empty"/>.</param>
    public static RelativePath Parse(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        PathSegments.Validate(path);
        if (path.Length == 0 || path == ".")
            return Empty;
        if (PathSegments.IsSeparator(path[0]) || (path.Length >= 2 && char.IsAsciiLetter(path[0]) && path[1] == ':'))
            throw new ArgumentException("A relative path cannot be rooted or drive-relative.", nameof(path));
        return new(PathSegments.Normalize(PathSegments.Parse(path), allowLeadingParents: true));
    }

    /// <summary>Creates a one-component relative path without interpreting directory separators in the component.</summary>
    /// <param name="segment">A non-empty filename or path component. It cannot be <c>.</c>, <c>..</c>, or contain NUL; directory separators are preserved literally.</param>
    public static RelativePath Raw(string segment)
    {
        ArgumentNullException.ThrowIfNull(segment);
        if (segment is "" or "." or ".." || segment.Contains('\0'))
            throw new ArgumentException("A raw path segment must be non-empty, cannot be '.' or '..', and cannot contain NUL.", nameof(segment));
        return new([segment]);
    }

    /// <summary>Renders the normalized path with forward slashes, regardless of the current platform.</summary>
    public string UnixPath => Render('/');
    /// <summary>Renders the normalized path with backslashes, regardless of the current platform.</summary>
    public string WindowsPath => Render('\\');
    /// <summary>The final path component, or <see langword="null"/> for a root or empty path.</summary>
    public string? Name => Segments.Length == 0 ? null : Segments[^1];
    /// <summary>The final component's extension, including its leading period; empty when it has none.</summary>
    public string Extension => PathSegments.Extension(Name);
    /// <summary>The final component without its last extension; <see langword="null"/> for <see cref="Empty"/>.</summary>
    public string? NameWithoutExtension => PathSegments.NameWithoutExtension(Name);
    /// <summary>The containing relative path, or <see langword="null"/> when fewer than two components remain.</summary>
    public RelativePath? Parent => Segments.Length <= 1 ? null : new(Segments[..^1]);
    /// <summary>Renders the value as one quoted command-line argument.</summary>
    public string QuotedArgument() => ToString().QuotedArgument();

    /// <summary>Combines and normalizes two relative paths, preserving valid leading parent traversal.</summary>
    /// <param name="left">The base relative path.</param>
    /// <param name="right">The relative path appended to it.</param>
    public static RelativePath operator /(RelativePath left, RelativePath right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return new(PathSegments.Normalize([.. left.Segments, .. right.Segments], allowLeadingParents: true));
    }

    /// <summary>Parses and appends relative path text.</summary>
    /// <param name="left">The base relative path.</param>
    /// <param name="right">Relative path text; rooted, drive-relative, and NUL-containing values are rejected.</param>
    public static RelativePath operator /(RelativePath left, string right) => left / Parse(right);
    /// <summary>Renders the path using the current operating system's directory separator.</summary>
    public static implicit operator string(RelativePath path) => path.Render(Path.DirectorySeparatorChar);
    /// <inheritdoc />
    public override string ToString() => Render(Path.DirectorySeparatorChar);

    /// <summary>Compares normalized path structure using ordinal segment equality.</summary>
    public bool Equals(RelativePath? other) => other is not null && PathSegments.Equal(Segments, other.Segments);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        PathSegments.AddToHash(ref hash, Segments);
        return hash.ToHashCode();
    }

    string Render(char separator) => Segments.Length == 0 ? "." : string.Join(separator, Segments);
}
