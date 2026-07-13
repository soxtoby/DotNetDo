using System.Collections.Concurrent;
using System.Text.Json;
using Serilog;
using static DotNetDo.Tools;

namespace DotNetDo;

/// <summary>A command supplied by a .NET local tool package whose await produces a semantic result.</summary>
public abstract record PackageToolCommand<TResult> : ToolCommand<TResult>
{
    /// <summary>Creates a local package-tool command with its manifest package ID and executable command name.</summary>
    protected PackageToolCommand(string packageId, string commandName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(commandName);
        PackageId = packageId;
        CommandName = commandName;
    }

    /// <summary>The package ID expected in the applicable local tool manifest.</summary>
    public string PackageId { get; }
    /// <summary>The command exposed by the package tool.</summary>
    public string CommandName { get; }
    /// <summary>The local-tool invocation rendered before configured arguments.</summary>
    protected override string CommandPrefix => $"dotnet tool run {CommandName}";

    /// <inheritdoc />
    protected sealed override async Task<ExecResult> ExecuteCommandAsync()
    {
        try
        {
            return await Do.Exec(this);
        }
        catch (ExecFailedException exception) when (PackageToolRunner.IsResolverFailure(exception.Result))
        {
            await Do.Exec(DotNet.ToolRestore, new ExecOptions { WorkingDirectory = Do.RootDirectory });
            return await Do.Exec(this);
        }
    }
}

/// <summary>A one-off package tool command whose await returns its successful raw process result.</summary>
public sealed record PackageToolCommand : PackageToolCommand<ExecResult>
{
    /// <summary>Creates a local package-tool command with its manifest package ID and executable command name.</summary>
    public PackageToolCommand(string packageId, string commandName) : base(packageId, commandName) { }

    /// <inheritdoc />
    protected override ExecResult ReadResult(ExecResult result) => result;
}

public static partial class Do
{
    /// <summary>Runs a declared local package tool without automatically restoring it.</summary>
    public static ExecProcess Exec<TResult>(PackageToolCommand<TResult> command, ExecOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(command);
        PackageToolManifests.Require(command, RootDirectory);
        var process = Exec((ToolCommand)command, options);
        _ = DiagnoseMissingRestoreAsync(process.Completed, command.PackageId);
        return process;
    }

    static async Task DiagnoseMissingRestoreAsync(Task<ExecResult> completed, string packageId)
    {
        var result = await completed;
        if (PackageToolRunner.IsResolverFailure(result))
            Log.Error("Package tool {PackageId} is declared but unavailable. Run 'dotnet tool restore' first", packageId);
    }
}

static class PackageToolRunner
{
    public static bool IsResolverFailure(ExecResult result) =>
        result.AllOutput.Any(line =>
            line.Message.Contains("dotnet tool restore", StringComparison.OrdinalIgnoreCase)
            || line.Message.Contains("cannot find a tool", StringComparison.OrdinalIgnoreCase));
}

static class PackageToolManifests
{
    static readonly ConcurrentDictionary<AbsolutePath, CachedManifest> Cache = new();

    public static void Require<TResult>(PackageToolCommand<TResult> command, AbsolutePath root)
    {
        foreach (var path in CandidatePaths(root))
        {
            if (!path.IsExistingFile)
                continue;

            var manifest = Read(path);
            if (manifest.Packages.Contains(command.PackageId))
                return;
            if (manifest.IsRoot)
                break;
        }

        var manifestPath = root / ".config/dotnet-tools.json";
        var instructions = manifestPath.IsExistingFile
            ? $"Run 'dotnet tool install {command.PackageId}' from '{root}'."
            : $"Run 'dotnet new tool-manifest' and 'dotnet tool install {command.PackageId}' from '{root}'.";
        Log.Error("Package tool {PackageId} is not declared. {Instructions}", command.PackageId, instructions);
        throw new PackageToolNotDeclaredException(command.PackageId, root, instructions);
    }

    static IEnumerable<AbsolutePath> CandidatePaths(AbsolutePath root)
    {
        for (var directory = root; directory is not null; directory = directory.Parent)
            yield return directory / ".config/dotnet-tools.json";
    }

    static Manifest Read(AbsolutePath path)
    {
        var info = new FileInfo(path);
        if (Cache.TryGetValue(path, out var cached) && cached.LastWriteTimeUtc == info.LastWriteTimeUtc && cached.Length == info.Length)
            return cached.Manifest;

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            var isRoot = root.TryGetProperty("isRoot", out var isRootElement) && isRootElement.GetBoolean();
            var packages = root.TryGetProperty("tools", out var tools)
                ? tools.EnumerateObject().Select(property => property.Name).ToHashSet(StringComparer.OrdinalIgnoreCase)
                : throw new InvalidDataException("The manifest has no 'tools' object.");
            var manifest = new Manifest(isRoot, packages);
            Cache[path] = new CachedManifest(info.LastWriteTimeUtc, info.Length, manifest);
            return manifest;
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException or InvalidOperationException)
        {
            throw new ToolManifestException(path, exception);
        }
    }

    sealed record Manifest(bool IsRoot, HashSet<string> Packages);
    sealed record CachedManifest(DateTime LastWriteTimeUtc, long Length, Manifest Manifest);
}

/// <summary>Indicates that a package tool is absent from every manifest in scope.</summary>
public sealed class PackageToolNotDeclaredException(string packageId, AbsolutePath root, string instructions)
    : Exception($"Package tool '{packageId}' is not declared in the tool manifests in scope from '{root}'. {instructions}")
{
    /// <summary>The missing package ID.</summary>
    public string PackageId { get; } = packageId;
    /// <summary>The DotNetDo root from which manifest discovery began.</summary>
    public AbsolutePath Root { get; } = root;
}

/// <summary>Indicates that a discovered local tool manifest is malformed.</summary>
public sealed class ToolManifestException(AbsolutePath manifestPath, Exception innerException)
    : Exception($"Tool manifest '{manifestPath}' is invalid.", innerException)
{
    /// <summary>The invalid manifest path.</summary>
    public AbsolutePath ManifestPath { get; } = manifestPath;
}
