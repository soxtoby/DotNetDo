namespace DotNetDo;

/// <summary>An immutable, normalized path relative to an unspecified base.</summary>
public sealed record RelativePath
{
    internal RelativePath(string[] segments) => Segments = segments;

    internal string[] Segments { get; }

    /// <summary>Returns a fresh value so later <c>with</c> customization cannot affect other callers.</summary>
    public static RelativePath Empty { get; } = new([]);

    /// <summary>Validates and converts textual input into the normalized value.</summary>
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

    /// <summary>Raw.</summary>
    public static RelativePath Raw(string segment)
    {
        ArgumentNullException.ThrowIfNull(segment);
        if (segment is "" or "." or ".." || segment.Contains('\0'))
            throw new ArgumentException("A raw path segment must be non-empty, cannot be '.' or '..', and cannot contain NUL.", nameof(segment));
        return new([segment]);
    }

    /// <summary>Renders the normalized path using the requested directory separator.</summary>
    public string UnixPath => Render('/');
    /// <summary>Renders the normalized path using the requested directory separator.</summary>
    public string WindowsPath => Render('\\');
    /// <summary>The final path component, or <see langword="null"/> for a root or empty path.</summary>
    public string? Name => Segments.Length == 0 ? null : Segments[^1];
    /// <summary>Extension.</summary>
    public string Extension => PathSegments.Extension(Name);
    /// <summary>Name without extension.</summary>
    public string? NameWithoutExtension => PathSegments.NameWithoutExtension(Name);
    /// <summary>Returns a fresh value so later <c>with</c> customization cannot affect other callers.</summary>
    public RelativePath? Parent => Segments.Length <= 1 ? null : new(Segments[..^1]);
    /// <summary>Renders the value as one quoted command-line argument.</summary>
    public string QuotedArgument() => ToString().QuotedArgument();

    /// <summary>Exposes the configured value or operation to script authors.</summary>
    public static RelativePath operator /(RelativePath left, RelativePath right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return new(PathSegments.Normalize([.. left.Segments, .. right.Segments], allowLeadingParents: true));
    }

    /// <summary>Validates and converts textual input into the normalized value.</summary>
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
