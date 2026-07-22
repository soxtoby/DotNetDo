using System.Text.Json.Serialization;
using Tomlyn.Model;
using Tomlyn.Serialization;
using static DotNetDo.Tools;

namespace DotNetDo;

public static partial class Do
{
    static readonly WorkspaceRoot WorkspaceRoot = new();

    /// <summary>Exposes the configured value or operation to task authors.</summary>
    public static AbsolutePath WorkingDirectory { get => AbsolutePath.Parse(Environment.CurrentDirectory); set => Directory.SetCurrentDirectory(value); }

    /// <summary>Exposes the configured value or operation to task authors.</summary>
    public static AbsolutePath RootDirectory => WorkspaceRoot.Resolve(WorkingDirectory);

    internal static RelativePath ScriptsPath => WorkspaceConfiguration.Load(RootDirectory).ScriptsPath;

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
            if ((directory / WorkspaceConfiguration.FileName).IsExistingFile)
                return _configured = directory;

        return workingDirectory;
    }
}

sealed record WorkspaceConfiguration
{
    public const string FileName = "dotnetdo.toml";

    public required RelativePath ScriptsPath { get; init; }
    public RelativePath? SolutionPath { get; init; }
    public required IReadOnlyList<ToolInstall> Tools { get; init; }
    public required IReadOnlyDictionary<string, string[]> MetaTasks { get; init; }

    public static WorkspaceConfiguration Load(AbsolutePath rootDirectory)
    {
        var configurationFile = rootDirectory / FileName;
        if (!configurationFile.IsExistingFile)
            return new()
                {
                    ScriptsPath = RelativePath.Parse("scripts"),
                    Tools = [],
                    MetaTasks = new Dictionary<string, string[]>(StringComparer.Ordinal)
                };

        try
        {
            var document = configurationFile.ReadToml<WorkspaceConfigurationDocument>()
                ?? throw new InvalidOperationException("TOML document produced no configuration.");

            foreach (var (key, value) in document.ExtensionData)
                if (value is not TomlTable)
                    throw new DotNetDoConfigurationException($"Unknown DotNetDo setting '{key}' in '{configurationFile}'.");

            return new()
                {
                    ScriptsPath = ReadRelativePath(document.ScriptsPath, configurationFile, "scripts-path") ?? RelativePath.Parse("scripts"),
                    SolutionPath = ReadRelativePath(document.SolutionPath, configurationFile, "solution-path"),
                    Tools = ReadTools(document.Tools, configurationFile),
                    MetaTasks = ReadMetaTasks(document.Tasks, configurationFile)
                };
        }
        catch (DotNetDoConfigurationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new DotNetDoConfigurationException($"Invalid DotNetDo configuration in '{configurationFile}'.", exception);
        }
    }

    static List<ToolInstall> ReadTools(string[]? toolNames, AbsolutePath configurationFile)
    {
        if (toolNames is null)
            return [];

        var tools = new List<ToolInstall>(toolNames.Length);
        var seenTools = new HashSet<string>(StringComparer.Ordinal);

        foreach (var toolName in toolNames)
        {
            var tool = FindTool(toolName)
                ?? throw new DotNetDoConfigurationException($"Unknown tool '{toolName}' in '{configurationFile}'.");

            if (!seenTools.Add(toolName))
                throw new DotNetDoConfigurationException($"Duplicate tool '{toolName}' in '{configurationFile}'.");

            tools.Add(tool);
        }

        return tools;
    }

    static ToolInstall? FindTool(string name) => name switch
        {
            Azure.ToolName => Azure.Install,
            _ => null,
        };

    static Dictionary<string, string[]> ReadMetaTasks(Dictionary<string, object?> tasks, AbsolutePath configurationFile)
    {
        var result = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var (name, value) in tasks)
        {
            if (!TaskName.IsValid(name))
                throw new DotNetDoConfigurationException($"Invalid meta-task name '{name}' in '{configurationFile}'. {TaskName.InvalidMessage}");

            var invocations = value switch
                {
                    string invocation => [invocation],
                    TomlArray array when array.All(item => item is string) => array.Cast<string>().ToArray(),
                    _ => throw new DotNetDoConfigurationException($"Meta-task '{name}' in '{configurationFile}' must be a string or an array of strings.")
                };

            if (invocations.Length == 0 || invocations.Any(string.IsNullOrWhiteSpace))
                throw new DotNetDoConfigurationException($"Meta-task '{name}' in '{configurationFile}' must contain at least one non-empty invocation.");

            result.Add(name, invocations);
        }

        return result;
    }

    static RelativePath? ReadRelativePath(string? path, AbsolutePath configurationFile, string key)
    {
        if (path is null)
            return null;
        if (string.IsNullOrWhiteSpace(path))
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

    public static RelativePath ParseRootRelativePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("A root-relative path must be non-empty.", nameof(path));
        var relativePath = RelativePath.Parse(path);
        return relativePath.Segments.FirstOrDefault() == ".."
            ? throw new ArgumentException("The path escapes the root directory.", nameof(path))
            : relativePath;
    }

    sealed class WorkspaceConfigurationDocument
    {
        [JsonPropertyName("scripts-path")]
        public string? ScriptsPath { get; init; }

        [JsonPropertyName("solution-path")]
        public string? SolutionPath { get; init; }

        [JsonPropertyName("tools")]
        public string[]? Tools { get; init; }

        [JsonPropertyName("tasks")]
        public Dictionary<string, object?> Tasks { get; init; } = [];

        [TomlExtensionData]
        public Dictionary<string, object?> ExtensionData { get; init; } = [];
    }
}

sealed class DotNetDoConfigurationException : Exception
{
    public DotNetDoConfigurationException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}