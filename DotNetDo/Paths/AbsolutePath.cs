using System.Diagnostics.CodeAnalysis;

namespace DotNetDo;

public sealed partial record AbsolutePath
{
    readonly PathRoot _root;
    readonly string[] _segments;

    AbsolutePath(PathRoot root, string[] segments)
    {
        _root = root;
        _segments = segments;
    }

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

    public string UnixPath => Render('/');
    public string WindowsPath => Render('\\');
    public string? Name => _segments.Length == 0 ? null : _segments[^1];
    public string Extension => PathSegments.Extension(Name);
    public string? NameWithoutExtension => PathSegments.NameWithoutExtension(Name);
    public AbsolutePath? Parent => _segments.Length == 0 ? null : new(_root, _segments[..^1]);
    public AbsolutePath Root => new(_root, []);
    [MemberNotNullWhen(false, nameof(Name), nameof(Parent))]
    public bool IsRoot => _segments.Length == 0;

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

    public static AbsolutePath operator /(AbsolutePath left, string right) => left / RelativePath.Parse(right);
    public static implicit operator string(AbsolutePath path) => path.Render(Path.DirectorySeparatorChar);
    public override string ToString() => Render(Path.DirectorySeparatorChar);

    public bool IsWithin(AbsolutePath directory) => _root == directory._root && _segments.SequenceStartsWith(directory._segments);

    public bool Equals(AbsolutePath? other) => other is not null && _root == other._root && PathSegments.Equal(_segments, other._segments);

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
