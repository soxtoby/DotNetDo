using Tomlyn;
using Tomlyn.Model;

namespace DotNetDo;

public static partial class Do
{
    /// <summary>Exposes the configured value or operation to script authors.</summary>
    public static AbsolutePath WorkingDirectory
    {
        get => AbsolutePath.Parse(Environment.CurrentDirectory);
        set => Directory.SetCurrentDirectory(value);
    }

    /// <summary>Exposes the configured value or operation to script authors.</summary>
    public static AbsolutePath RootDirectory
    {
        get
        {
            if (field is not null)
                return field;

            var workingDirectory = WorkingDirectory;
            for (var directory = workingDirectory; directory is not null; directory = directory.Parent)
                if ((directory / "dotnetdo.toml").IsExistingFile)
                    return field = directory;

            return workingDirectory;
        }
    }

    internal static RelativePath ScriptsPath => WorkspaceConfiguration.ReadScriptsPath(RootDirectory);

    internal static AbsolutePath ScriptsDirectory => RootDirectory / ScriptsPath;
}

static class WorkspaceConfiguration
{
    public static RelativePath ReadScriptsPath(AbsolutePath rootDirectory)
    {
        var configurationFile = rootDirectory / "dotnetdo.toml";
        if (!configurationFile.IsExistingFile)
            return RelativePath.Parse("scripts");

        TomlTable configuration;
        try
        {
            configuration = TomlSerializer.Deserialize<TomlTable>(File.ReadAllText(configurationFile))
                ?? throw new InvalidOperationException("TOML document produced no configuration.");
        }
        catch (Exception exception)
        {
            throw new DotNetDoConfigurationException($"Invalid DotNetDo configuration in '{configurationFile}'.", exception);
        }

        foreach (var (key, value) in configuration)
            if (value is not TomlTable && key != "scripts-path")
                throw new DotNetDoConfigurationException($"Unknown DotNetDo setting '{key}' in '{configurationFile}'.");

        if (!configuration.TryGetValue("scripts-path", out var configuredPath))
            return RelativePath.Parse("scripts");
        if (configuredPath is not string path || string.IsNullOrWhiteSpace(path))
            throw new DotNetDoConfigurationException($"DotNetDo setting 'scripts-path' in '{configurationFile}' must be a non-empty relative path.");

        try
        {
            var relativePath = RelativePath.Parse(path);
            return relativePath.Segments.FirstOrDefault() == ".." 
                ? throw new ArgumentException("The scripts path escapes the root directory.") 
                : relativePath;
        }
        catch (ArgumentException exception)
        {
            throw new DotNetDoConfigurationException($"DotNetDo setting 'scripts-path' in '{configurationFile}' must remain within the root directory.", exception);
        }
    }
}

sealed class DotNetDoConfigurationException : Exception
{
    public DotNetDoConfigurationException(string message, Exception? innerException = null) : base(message, innerException) { }
}
