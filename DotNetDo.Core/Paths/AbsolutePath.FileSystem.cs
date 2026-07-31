using Microsoft.Extensions.FileSystemGlobbing;

namespace DotNetDo;

public sealed partial record AbsolutePath
{
    /// <summary>Whether a file or directory currently exists at this path.</summary>
    public bool Exists => IsExistingFile || IsExistingDirectory;
    /// <summary>Whether an existing regular file occupies this path.</summary>
    public bool IsExistingFile => File.Exists(this);
    /// <summary>Whether an existing directory occupies this path.</summary>
    public bool IsExistingDirectory => Directory.Exists(this);

    /// <summary>Creates this directory and any missing parents, returning this path.</summary>
    public AbsolutePath EnsureDirectoryExists()
    {
        Directory.CreateDirectory(this);
        return this;
    }

    /// <summary>Delete and recreate this directory.</summary>
    public AbsolutePath RecreateDirectory()
    {
        if (IsExistingDirectory)
            Directory.Delete(this, recursive: true);

        Directory.CreateDirectory(this);
        return this;
    }

    /// <summary>Returns files beneath this directory matched by the ordered include and exclude patterns.</summary>
    /// <param name="pattern">A glob relative to this directory. Prefix with <c>!</c> to exclude earlier matches.</param>
    /// <param name="options">Optional glob comparison settings; defaults to ordinal matching.</param>
    public AbsolutePath[] GlobFiles(string pattern, GlobOptions? options = null) => GlobFiles([pattern], options);

    /// <summary>Returns files beneath this directory matched by the ordered include and exclude patterns.</summary>
    /// <param name="patterns">Ordered directory-relative globs. Exclusions begin with <c>!</c> and remove earlier matches.</param>
    /// <param name="options">Optional glob comparison settings; defaults to ordinal matching.</param>
    public AbsolutePath[] GlobFiles(IEnumerable<string> patterns, GlobOptions? options = null) =>
        CreateMatcher(patterns, options)
            .GetResultsInFullPath(this)
            .Order(StringComparer.Ordinal)
            .Select(Parse)
            .ToArray();

    /// <summary>Returns directories beneath this directory matched by the ordered include and exclude patterns.</summary>
    /// <param name="pattern">A glob relative to this directory. Prefix with <c>!</c> to exclude earlier matches.</param>
    /// <param name="options">Optional glob comparison settings; defaults to ordinal matching.</param>
    public AbsolutePath[] GlobDirectories(string pattern, GlobOptions? options = null) => GlobDirectories([pattern], options);

    /// <summary>Returns directories beneath this directory matched by the ordered include and exclude patterns.</summary>
    /// <param name="patterns">Ordered directory-relative globs. Exclusions begin with <c>!</c> and remove earlier matches.</param>
    /// <param name="options">Optional glob comparison settings; defaults to ordinal matching.</param>
    public AbsolutePath[] GlobDirectories(IEnumerable<string> patterns, GlobOptions? options = null)
    {
        var candidates = Directory
            .EnumerateDirectories(this, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(this, path).Replace('\\', '/'));
        return CreateMatcher(patterns, options)
            .Match(candidates)
            .Files
            .OrderBy(match => match.Path, StringComparer.Ordinal)
            .Select(match => this / match.Path)
            .ToArray();
    }

    static Matcher CreateMatcher(IEnumerable<string> patterns, GlobOptions? options)
    {
        var matcher = new Matcher((options ?? new()).Comparison, preserveFilterOrder: true);
        foreach (var pattern in patterns)
        {
            if (pattern.StartsWith('!'))
                matcher.AddExclude(pattern[1..]);
            else
                matcher.AddInclude(pattern.StartsWith("\\!") ? pattern[1..] : pattern);
        }

        return matcher;
    }

    /// <summary>Copies this file or directory to the exact destination path.</summary>
    /// <param name="destination">The exact destination filename or directory path.</param>
    /// <param name="options">Controls replacement of existing items and creation of missing parent directories.</param>
    public AbsolutePath CopyTo(AbsolutePath destination, TransferOptions? options = null) =>
        Copy(destination, options, into: false);

    /// <summary>Copies this item beneath the supplied destination directory using its current name.</summary>
    /// <param name="directory">The directory that will contain a copy under this item's current name.</param>
    /// <param name="options">Controls replacement of existing items and creation of missing directories.</param>
    public AbsolutePath CopyInto(AbsolutePath directory, TransferOptions? options = null) =>
        Copy(directory, options, into: true);

    /// <summary>Moves this file or directory to the exact destination path.</summary>
    /// <param name="destination">The exact destination filename or directory path.</param>
    /// <param name="options">Controls replacement of existing items and creation of missing parent directories.</param>
    public AbsolutePath MoveTo(AbsolutePath destination, TransferOptions? options = null) =>
        Move(destination, options, into: false);

    /// <summary>Moves this item beneath the supplied destination directory using its current name.</summary>
    /// <param name="directory">The directory that will contain this item under its current name.</param>
    /// <param name="options">Controls replacement of existing items and creation of missing directories.</param>
    public AbsolutePath MoveInto(AbsolutePath directory, TransferOptions? options = null) =>
        Move(directory, options, into: true);

    /// <summary>Deletes the file or directory.</summary>
    public void Delete()
    {
        if (IsExistingDirectory)
            Directory.Delete(this, recursive: true);
        else
            File.Delete(this);
    }

    AbsolutePath Copy(AbsolutePath destination, TransferOptions? options, bool into)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (IsRoot)
            throw new InvalidOperationException($"Cannot copy the root directory '{this}'.");
        if (destination.IsRoot && !into)
            throw new ArgumentException($"Cannot copy over the root directory '{destination}'.", nameof(destination));

        options ??= new();
        var finalPath = into ? destination / Name : destination;
        
        if (options.CreateDirectories)
            finalPath.Parent.EnsureDirectoryExists();
        else if (!finalPath.Parent.IsExistingDirectory)
            throw new DirectoryNotFoundException($"Destination directory '{finalPath.Parent}' does not exist.");
        
        CopyEntry(this, finalPath, options);
        
        return finalPath;
    }

    AbsolutePath Move(AbsolutePath destination, TransferOptions? options, bool into)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (IsRoot)
            throw new InvalidOperationException($"Cannot move the root directory '{this}'.");
        if (destination.IsRoot && !into)
            throw new ArgumentException($"Cannot move over the root directory '{destination}'.", nameof(destination));

        options ??= new();
        var finalPath = into ? destination / Name : destination;

        if (IsExistingDirectory && finalPath.IsWithin(this))
            throw new IOException("Cannot move a directory into itself.");
        
        if (IsExistingDirectory && finalPath.IsExistingDirectory && options.Overwrite)
            return CopyThenDelete(finalPath, options);
        
        if (options.CreateDirectories)
            finalPath.Parent.EnsureDirectoryExists();

        try
        {
            MoveNative(finalPath, options.Overwrite);
            return finalPath;
        }
        catch (IOException exception) when (IsCrossDevice(exception))
        {
            return CopyThenDelete(finalPath, options);
        }
    }

    void MoveNative(AbsolutePath destination, bool overwrite)
    {
        if (IsExistingDirectory)
            Directory.Move(this, destination);
        else
            File.Move(this, destination, overwrite);
    }

    AbsolutePath CopyThenDelete(AbsolutePath destination, TransferOptions options)
    {
        CopyEntry(this, destination, options);
        Delete();
        return destination;
    }

    static void CopyEntry(AbsolutePath source, AbsolutePath destination, TransferOptions options)
    {
        if (source.IsExistingDirectory)
        {
            var files = source.GlobFiles("**/*");
            var directories = source.GlobDirectories("**/*");

            if (destination.IsExistingDirectory && !options.Overwrite)
                throw new IOException($"The destination '{destination}' already exists.");

            destination.EnsureDirectoryExists();
            
            foreach (var directory in directories)
                (destination / source.RelativePathTo(directory)).EnsureDirectoryExists();

            foreach (var file in files)
                File.Copy(file, destination / source.RelativePathTo(file), overwrite: options.Overwrite);
        }
        else
        {
            File.Copy(source, destination, overwrite: options.Overwrite);
        }
    }

    static bool IsCrossDevice(IOException exception) => (exception.HResult & 0xffff) is 17 or 18;
}

/// <summary>Controls path glob matching.</summary>
public sealed record GlobOptions
{
    /// <summary>The comparison used when matching glob patterns.</summary>
    public StringComparison Comparison { get; init; } = StringComparison.OrdinalIgnoreCase;
}

/// <summary>Controls overwrite and directory-creation behavior for copies and moves.</summary>
public sealed record TransferOptions
{
    /// <summary>Whether an existing destination may be replaced.</summary>
    public bool Overwrite { get; init; }
    /// <summary>Whether missing destination directories are created.</summary>
    public bool CreateDirectories { get; init; }
}
