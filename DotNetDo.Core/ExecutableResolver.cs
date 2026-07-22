namespace DotNetDo;

static class ExecutableResolver
{
    public static AbsolutePath? Find(string command, AbsolutePath? workingDirectory = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);

        var extensions = Extensions(command);

        return AbsolutePath.TryParse(command, out var absolutePath) ? FindAbsolutePathCommand(absolutePath, extensions)
            : Path.GetFileName(command) != command ? FindRelativePathCommand(command, workingDirectory, extensions)
            : FindGlobalPathCommand(command, extensions);
    }

    static AbsolutePath? FindAbsolutePathCommand(AbsolutePath absolutePath, string[] extensions) => Candidates(absolutePath, extensions).FirstOrDefault(IsExecutable);

    static AbsolutePath? FindRelativePathCommand(string command, AbsolutePath? workingDirectory, string[] extensions)
    {
        try
        {
            var path = (workingDirectory ?? Do.WorkingDirectory) / command;
            return Candidates(path, extensions).FirstOrDefault(IsExecutable);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    static AbsolutePath? FindGlobalPathCommand(string command, string[] extensions) =>
        EnvironmentPath.Read(EnvironmentVariableTarget.Process)
            .Select(entry => AbsolutePath.TryParse(entry, out var directory) ? directory : null)
            .WhereNotNull()
            .SelectMany(directory => Candidates(directory / command, extensions))
            .FirstOrDefault(IsExecutable);

    public static bool IsBatchFile(AbsolutePath path) =>
        path.Extension.Equals(".cmd", StringComparison.OrdinalIgnoreCase)
        || path.Extension.Equals(".bat", StringComparison.OrdinalIgnoreCase);

    static string[] Extensions(string command) =>
        OperatingSystem.IsWindows() && !Path.HasExtension(command)
            ? (Environment.GetEnvironmentVariable("PATHEXT") ?? ".COM;.EXE;.BAT;.CMD")
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : [""];

    static IEnumerable<AbsolutePath> Candidates(AbsolutePath path, IEnumerable<string> extensions) =>
        extensions.Select(extension => path.Parent! / (path.Name + extension));

    static bool IsExecutable(AbsolutePath path) =>
        path.IsExistingFile
        && (OperatingSystem.IsWindows()
            || (File.GetUnixFileMode(path) & (UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute)) != 0);
}

static class EnvironmentPath
{
    public static string[] Read(EnvironmentVariableTarget target) =>
        (Environment.GetEnvironmentVariable("PATH", target) ?? "")
        .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}