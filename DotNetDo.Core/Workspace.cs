using Tomlyn;
using Tomlyn.Model;

namespace DotNetDo;

public static partial class Do
{
    static readonly WorkspaceRoot WorkspaceRoot = new();

    /// <summary>Exposes the configured value or operation to task authors.</summary>
    public static AbsolutePath WorkingDirectory
    {
        get => AbsolutePath.Parse(Environment.CurrentDirectory);
        set => Directory.SetCurrentDirectory(value);
    }

    /// <summary>Exposes the configured value or operation to task authors.</summary>
    public static AbsolutePath RootDirectory => WorkspaceRoot.Resolve(WorkingDirectory);

    internal static RelativePath ScriptsPath => WorkspaceConfiguration.ReadScriptsPath(RootDirectory);

    internal static AbsolutePath ScriptsDirectory => RootDirectory / ScriptsPath;
}

sealed class WorkspaceRoot
{
    AbsolutePath? _configured;

    public AbsolutePath Resolve(AbsolutePath workingDirectory)
    {
        if (_configured is not null)
            return _configured;

        for (var directory = workingDirectory; directory is not null; directory = directory.Parent)
            if ((directory / "dotnetdo.toml").IsExistingFile)
                return _configured = directory;

        return workingDirectory;
    }
}

static class WorkspaceConfiguration
{
    public static RelativePath ReadScriptsPath(AbsolutePath rootDirectory) =>
        ReadRelativePath(Read(rootDirectory), rootDirectory, "scripts-path") ?? RelativePath.Parse("scripts");

    public static RelativePath? ReadSolutionPath(AbsolutePath rootDirectory) =>
        ReadRelativePath(Read(rootDirectory), rootDirectory, "solution-path");

    public static RelativePath ParseRootRelativePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("A root-relative path must be non-empty.", nameof(path));
        var relativePath = RelativePath.Parse(path);
        return relativePath.Segments.FirstOrDefault() == ".."
            ? throw new ArgumentException("The path escapes the root directory.", nameof(path))
            : relativePath;
    }

    static TomlTable Read(AbsolutePath rootDirectory)
    {
        var configurationFile = rootDirectory / "dotnetdo.toml";
        if (!configurationFile.IsExistingFile)
            return [];

        try
        {
            var configuration = TomlSerializer.Deserialize<TomlTable>(File.ReadAllText(configurationFile))
                ?? throw new InvalidOperationException("TOML document produced no configuration.");
            foreach (var (key, value) in configuration)
                if (value is not TomlTable && key is not "scripts-path" and not "solution-path")
                    throw new DotNetDoConfigurationException($"Unknown DotNetDo setting '{key}' in '{configurationFile}'.");
            return configuration;
        }
        catch (DotNetDoConfigurationException) { throw; }
        catch (Exception exception)
        {
            throw new DotNetDoConfigurationException($"Invalid DotNetDo configuration in '{configurationFile}'.", exception);
        }
    }

    static RelativePath? ReadRelativePath(TomlTable configuration, AbsolutePath rootDirectory, string key)
    {
        if (!configuration.TryGetValue(key, out var configuredPath))
            return null;
        var configurationFile = rootDirectory / "dotnetdo.toml";
        if (configuredPath is not string path || string.IsNullOrWhiteSpace(path))
            throw new DotNetDoConfigurationException($"DotNetDo setting '{key}' in '{configurationFile}' must be a non-empty relative path.");

        try
        {
            return ParseRootRelativePath(path);
        }
        catch (ArgumentException exception)
        {
            throw new DotNetDoConfigurationException($"DotNetDo setting '{key}' in '{configurationFile}' must remain within the root directory.", exception);
        }
    }
}

sealed class DotNetDoConfigurationException : Exception
{
    public DotNetDoConfigurationException(string message, Exception? innerException = null) : base(message, innerException) { }
}
