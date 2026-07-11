namespace DotNetDo;

public sealed record RelativePath
{
    internal RelativePath(string[] segments) => Segments = segments;

    internal string[] Segments { get; }

    public static RelativePath Empty { get; } = new([]);

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

    public static RelativePath Raw(string segment)
    {
        ArgumentNullException.ThrowIfNull(segment);
        if (segment is "" or "." or ".." || segment.Contains('\0'))
            throw new ArgumentException("A raw path segment must be non-empty, cannot be '.' or '..', and cannot contain NUL.", nameof(segment));
        return new([segment]);
    }

    public string UnixPath => Render('/');
    public string WindowsPath => Render('\\');
    public string? Name => Segments.Length == 0 ? null : Segments[^1];
    public string Extension => PathSegments.Extension(Name);
    public string? NameWithoutExtension => PathSegments.NameWithoutExtension(Name);
    public RelativePath? Parent => Segments.Length <= 1 ? null : new(Segments[..^1]);

    public static RelativePath operator /(RelativePath left, RelativePath right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return new(PathSegments.Normalize([.. left.Segments, .. right.Segments], allowLeadingParents: true));
    }

    public static RelativePath operator /(RelativePath left, string right) => left / Parse(right);
    public static implicit operator string(RelativePath path) => path.Render(Path.DirectorySeparatorChar);
    public override string ToString() => Render(Path.DirectorySeparatorChar);

    public bool Equals(RelativePath? other) => other is not null && PathSegments.Equal(Segments, other.Segments);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        PathSegments.AddToHash(ref hash, Segments);
        return hash.ToHashCode();
    }

    string Render(char separator) => Segments.Length == 0 ? "." : string.Join(separator, Segments);
}
