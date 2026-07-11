using Microsoft.Extensions.FileSystemGlobbing;

namespace DotNetDo;

public sealed partial record AbsolutePath
{
    public bool Exists => IsExistingFile || IsExistingDirectory;
    public bool IsExistingFile => File.Exists(this);
    public bool IsExistingDirectory => Directory.Exists(this);

    public AbsolutePath EnsureDirectoryExists()
    {
        Directory.CreateDirectory(this);
        return this;
    }

    public AbsolutePath[] GlobFiles(string pattern, GlobOptions? options = null) => GlobFiles([pattern], options);

    public AbsolutePath[] GlobFiles(IEnumerable<string> patterns, GlobOptions? options = null) =>
        CreateMatcher(patterns, options)
            .GetResultsInFullPath(this)
            .Select(Parse)
            .ToArray();

    public AbsolutePath[] GlobDirectories(string pattern, GlobOptions? options = null) => GlobDirectories([pattern], options);

    public AbsolutePath[] GlobDirectories(IEnumerable<string> patterns, GlobOptions? options = null)
    {
        var candidates = Directory
            .EnumerateDirectories(this, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(this, path).Replace('\\', '/'));
        return CreateMatcher(patterns, options)
            .Match(candidates)
            .Files
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

    public AbsolutePath CopyTo(AbsolutePath destination, TransferOptions? options = null) =>
        Copy(destination, options, into: false);

    public AbsolutePath CopyInto(AbsolutePath directory, TransferOptions? options = null) =>
        Copy(directory, options, into: true);

    public AbsolutePath MoveTo(AbsolutePath destination, TransferOptions? options = null) =>
        Move(destination, options, into: false);

    public AbsolutePath MoveInto(AbsolutePath directory, TransferOptions? options = null) =>
        Move(directory, options, into: true);

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
            finalPath.Parent!.EnsureDirectoryExists();
        else if (!finalPath.Parent!.IsExistingDirectory)
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
            finalPath.Parent!.EnsureDirectoryExists();

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

public sealed record GlobOptions
{
    public StringComparison Comparison { get; init; } = StringComparison.OrdinalIgnoreCase;
}

public sealed record TransferOptions
{
    public bool Overwrite { get; init; }
    public bool CreateDirectories { get; init; }
}
