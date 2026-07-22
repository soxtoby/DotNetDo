using System.Diagnostics.CodeAnalysis;

namespace DotNetDo;

/// <summary>An immutable, normalized absolute path with explicit root semantics.</summary>
public sealed partial record AbsolutePath
{
    readonly PathRoot _root;
    readonly string[] _segments;

    AbsolutePath(PathRoot root, string[] segments)
    {
        _root = root;
        _segments = segments;
    }

    /// <summary>Converts textual input into the normalized value, returning whether the input was valid.</summary>
    public static bool TryParse(string? path, [NotNullWhen(true)] out AbsolutePath? result)
    {
        try
        {
            result = path is null ? null : Parse(path);
        }
        catch (ArgumentException)
        {
            result = null;
        }

        return result is not null;
    }

    /// <summary>Validates and converts textual input into the normalized value.</summary>
    public static AbsolutePath Parse(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        PathSegments.Validate(path);
        if (path.Length == 0)
            throw new ArgumentException("An absolute path cannot be empty.", nameof(path));

        if (path.Length >= 2 && PathSegments.IsSeparator(path[0]) && PathSegments.IsSeparator(path[1]))
        {
            var pieces = PathSegments.Parse(path[2..]);
            if (pieces.Length < 2 || pieces[0] is "." or ".." || pieces[1] is "." or "..")
                throw new ArgumentException("A UNC path requires non-empty server and share components.", nameof(path));
            return new(new(PathRootKind.Unc, pieces[0], pieces[1]), PathSegments.Normalize(pieces[2..], allowLeadingParents: false));
        }

        if (path[0] == '/')
            return new(new(PathRootKind.Unix), PathSegments.Normalize(PathSegments.Parse(path[1..]), allowLeadingParents: false));

        if (path.Length >= 3 && char.IsAsciiLetter(path[0]) && path[1] == ':' && PathSegments.IsSeparator(path[2]))
            return new(new(PathRootKind.Drive, path[..2]), PathSegments.Normalize(PathSegments.Parse(path[3..]), allowLeadingParents: false));

        throw new ArgumentException("Path is not Unix-rooted, drive-rooted, or UNC-rooted.", nameof(path));
    }

    /// <summary>Renders the normalized path using the requested directory separator.</summary>
    public string UnixPath => Render('/');
    /// <summary>Renders the normalized path using the requested directory separator.</summary>
    public string WindowsPath => Render('\\');
    /// <summary>The final path component, or <see langword="null"/> for a root or empty path.</summary>
    public string? Name => _segments.Length == 0 ? null : _segments[^1];
    /// <summary>Extension.</summary>
    public string Extension => PathSegments.Extension(Name);
    /// <summary>Name without extension.</summary>
    public string? NameWithoutExtension => PathSegments.NameWithoutExtension(Name);
    /// <summary>Returns a fresh value so later <c>with</c> customization cannot affect other callers.</summary>
    public AbsolutePath? Parent => _segments.Length == 0 ? null : new(_root, _segments[..^1]);
    /// <summary>Returns a fresh value so later <c>with</c> customization cannot affect other callers.</summary>
    public AbsolutePath Root => new(_root, []);
    /// <summary>Gets or sets is root.</summary>
    [MemberNotNullWhen(false, nameof(Name), nameof(Parent))]
    public bool IsRoot => _segments.Length == 0;
    /// <summary>Renders the value as one quoted command-line argument.</summary>
    public string QuotedArgument() => ToString().QuotedArgument();

    /// <summary>Computes the lexical path from this location to another path on the same root.</summary>
    public RelativePath RelativePathTo(AbsolutePath path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (_root != path._root)
            throw new ArgumentException("Paths must have the same root.", nameof(path));

        var common = 0;
        while (common < _segments.Length && common < path._segments.Length && _segments[common] == path._segments[common])
            common++;

        return new([
                .. Enumerable.Repeat("..", _segments.Length - common),
                .. path._segments[common..]
            ]);
    }

    /// <summary>Exposes the configured value or operation to script authors.</summary>
    public static AbsolutePath operator /(AbsolutePath left, RelativePath right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        try
        {
            return new(left._root, PathSegments.Normalize([.. left._segments, .. right.Segments], allowLeadingParents: false));
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException("The relative path escapes the absolute root.", exception);
        }
    }

    /// <summary>Validates and converts textual input into the normalized value.</summary>
    public static AbsolutePath operator /(AbsolutePath left, string right) => left / RelativePath.Parse(right);
    /// <summary>Renders the path using the current operating system's directory separator.</summary>
    public static implicit operator string(AbsolutePath path) => path.Render(Path.DirectorySeparatorChar);
    /// <inheritdoc />
    public override string ToString() => Render(Path.DirectorySeparatorChar);

    /// <summary>Returns whether this path is equal to or nested beneath the supplied directory.</summary>
    public bool IsWithin(AbsolutePath directory) => _root == directory._root && _segments.SequenceStartsWith(directory._segments);

    /// <summary>Compares normalized path structure using ordinal segment equality.</summary>
    public bool Equals(AbsolutePath? other) => other is not null && _root == other._root && PathSegments.Equal(_segments, other._segments);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(_root);
        PathSegments.AddToHash(ref hash, _segments);
        return hash.ToHashCode();
    }

    string Render(char separator)
    {
        var prefix = _root.Kind switch
            {
                PathRootKind.Unix => separator.ToString(),
                PathRootKind.Drive => _root.First + separator,
                PathRootKind.Unc => new string(separator, 2) + _root.First + separator + _root.Second + separator,
                _ => throw new InvalidOperationException("Unknown path root kind.")
            };
        return _segments.Length == 0 ? prefix : prefix + string.Join(separator, _segments);
    }

    readonly record struct PathRoot(PathRootKind Kind, string First = "", string Second = "");

    enum PathRootKind { Unix, Drive, Unc }
}
