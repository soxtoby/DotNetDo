namespace DotNetDo;

public static partial class Do
{
    /// <summary>Creates a uniquely named directory under the platform's temporary directory.</summary>
    /// <param name="prefix">An optional filename-safe prefix. Directory separators and invalid filename characters are rejected.</param>
    public static AbsolutePath CreateTempDirectory(string? prefix = null)
    {
        ValidateTempPrefix(prefix);
        return AbsolutePath.Parse(Directory.CreateTempSubdirectory(prefix).FullName);
    }

    /// <summary>Creates a uniquely named empty file under the platform's temporary directory.</summary>
    /// <param name="prefix">An optional filename-safe prefix. Directory separators and invalid filename characters are rejected.</param>
    /// <param name="extension">A file extension of at least two characters, including its leading <c>.</c>; separators and invalid filename characters are rejected.</param>
    public static AbsolutePath CreateTempFile(string? prefix = null, string extension = ".tmp")
    {
        ValidateTempPrefix(prefix);
        if (extension.Length < 2 || extension[0] != '.' || extension.IndexOfAny(['/', '\\', ..Path.GetInvalidFileNameChars()]) >= 0)
            throw new ArgumentException("Extension must be a dot-prefixed file extension.", nameof(extension));

        var fileName = Path.ChangeExtension($"{prefix}{Path.GetRandomFileName()}", extension);
        var path = AbsolutePath.Parse(Path.Combine(Path.GetTempPath(), fileName));
        File.Create(path).Dispose(); // Creates an empty file
        return path;
    }

    static void ValidateTempPrefix(string? prefix)
    {
        if (prefix is not null && prefix.IndexOfAny(['/','\\', ..Path.GetInvalidFileNameChars()]) >= 0)
            throw new ArgumentException("Prefix contains invalid filename characters.", nameof(prefix));
    }
}
