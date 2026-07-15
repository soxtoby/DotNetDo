namespace DotNetDo;

static class PathSegments
{
    public static bool IsSeparator(char value) => value is '/' or '\\';

    public static string[] Parse(string path)
    {
        Validate(path);
        return path.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
    }

    public static void Validate(string path)
    {
        if (path.Contains('\0'))
            throw new ArgumentException("Paths cannot contain NUL.", nameof(path));
    }

    public static string[] Normalize(IEnumerable<string> segments, bool allowLeadingParents)
    {
        var result = new List<string>();
        foreach (var segment in segments)
        {
            switch (segment)
            {
                case ".":
                    break;
                case ".." when result.Count != 0 && result[^1] != "..":
                    result.RemoveAt(result.Count - 1);
                    break;
                case ".." when !allowLeadingParents:
                    throw new ArgumentException("Path traversal cannot escape an absolute root.");
                default:
                    result.Add(segment);
                    break;
            }
        }
        return [.. result];
    }

    public static string Extension(string? name)
    {
        if (name is null)
            return "";
        var index = name.LastIndexOf('.');
        return index <= 0 || index == name.Length - 1 ? "" : name[index..];
    }

    public static string? NameWithoutExtension(string? name)
    {
        if (name is null)
            return null;
        var extension = Extension(name);
        return extension.Length == 0 ? name : name[..^extension.Length];
    }

    public static bool Equal(string[] left, string[] right) => left.AsSpan().SequenceEqual(right);

    public static void AddToHash(ref HashCode hash, string[] segments)
    {
        foreach (var segment in segments)
            hash.Add(segment, StringComparer.Ordinal);
    }
}
