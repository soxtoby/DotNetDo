using System.IO.Compression;

namespace DotNetDo;

public sealed partial record AbsolutePath
{
    /// <summary>Creates a ZIP archive from this file or directory at the exact destination path.</summary>
    /// <param name="destination">The exact archive path. Its parent directory must exist.</param>
    /// <param name="options">Controls compression and replacement of an existing archive.</param>
    public AbsolutePath ZipTo(AbsolutePath destination, ZipOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (IsRoot)
            throw new InvalidOperationException($"Cannot archive the root directory '{this}'.");
        if (destination.IsRoot)
            throw new ArgumentException($"Cannot create an archive over the root directory '{destination}'.", nameof(destination));
        if (SameFileSystemPath(this, destination))
            throw new InvalidOperationException("The archive destination cannot be the source path.");

        var sourceIsDirectory = IsExistingDirectory;
        var sourceIsFile = IsExistingFile;
        if (!sourceIsDirectory && !sourceIsFile)
            throw new FileNotFoundException($"Archive source '{this}' does not exist.", this);
        if (sourceIsDirectory && IsSameOrDescendant(this, destination))
            throw new InvalidOperationException("The archive destination cannot be inside its source directory.");
        if (!destination.Parent.IsExistingDirectory)
            throw new DirectoryNotFoundException($"Destination directory '{destination.Parent}' does not exist.");

        options ??= new();
        if (destination.IsExistingDirectory)
            throw new IOException($"The archive destination '{destination}' is a directory.");
        if (destination.IsExistingFile && !options.Overwrite)
            throw new IOException($"The archive destination '{destination}' already exists.");

        var temporary = Do.CreateTempFile();
        try
        {
            using (var output = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                if (sourceIsDirectory)
                    ZipDirectoryTo(output, options.CompressionLevel);
                else
                    ZipFileTo(output, options.CompressionLevel);
            }

            temporary.MoveTo(destination, new() { Overwrite = options.Overwrite });
            return destination;
        }
        catch (Exception error)
        {
            try
            {
                temporary.Delete();
            }
            catch (Exception cleanupError)
            {
                throw new AggregateException(error, cleanupError);
            }

            throw;
        }
    }

    /// <summary>Extracts this ZIP archive into the exact destination directory.</summary>
    /// <param name="destination">The exact output directory. Its parent directory must exist.</param>
    /// <param name="options">Controls merging with existing destination entries.</param>
    public AbsolutePath UnzipTo(AbsolutePath destination, UnzipOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (destination.IsRoot)
            throw new InvalidOperationException($"Cannot extract an archive into the root directory '{destination}'.");
        if (!destination.Parent.IsExistingDirectory)
            throw new DirectoryNotFoundException($"Destination directory '{destination.Parent}' does not exist.");
        if (destination.IsExistingFile)
            throw new IOException($"The extraction destination '{destination}' is a file.");

        options ??= new();
        ZipFile.ExtractToDirectory(this, destination, options.Overwrite);
        return destination;
    }

    void ZipDirectoryTo(Stream destination, CompressionLevel? compressionLevel)
    {
        if (compressionLevel is { } level)
            ZipFile.CreateFromDirectory(this, destination, level, includeBaseDirectory: false);
        else
            ZipFile.CreateFromDirectory(this, destination);
    }

    void ZipFileTo(Stream destination, CompressionLevel? compressionLevel)
    {
        using var archive = new ZipArchive(destination, ZipArchiveMode.Create, leaveOpen: true);
        if (compressionLevel is { } level)
            archive.CreateEntryFromFile(this, Name!, level);
        else
            archive.CreateEntryFromFile(this, Name!);
    }

    static bool SameFileSystemPath(AbsolutePath left, AbsolutePath right) =>
        string.Equals(
            Path.GetFullPath(left),
            Path.GetFullPath(right),
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

    static bool IsSameOrDescendant(AbsolutePath directory, AbsolutePath path)
    {
        var relative = Path.GetRelativePath(directory, path);
        return relative == "." ||
               !Path.IsPathRooted(relative) &&
               relative != ".." &&
               !relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal);
    }
}

/// <summary>Controls ZIP archive creation.</summary>
public sealed record ZipOptions
{
    /// <summary>Whether an existing destination archive may be replaced after creation succeeds.</summary>
    public bool Overwrite { get; init; }

    /// <summary>The compression trade-off, or <see langword="null"/> to use the underlying implementation's default.</summary>
    public CompressionLevel? CompressionLevel { get; init; }
}

/// <summary>Controls ZIP archive extraction.</summary>
public sealed record UnzipOptions
{
    /// <summary>Whether existing destination files may be replaced.</summary>
    public bool Overwrite { get; init; }
}
