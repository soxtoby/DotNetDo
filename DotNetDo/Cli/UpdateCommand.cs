using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using NuGet.Versioning;
using static DotNetDo.Tools;

namespace DotNetDo.Cli;

static partial class UpdateCommand
{
    const string DotNetDoPackage = "DotNetDo";
    const string CorePackage = "DotNetDo.Core";
    const string Usage = "Usage: dotnet do :update [<package> | --all] [--prerelease]";

    public static Task<int> Run(string[] args) => Run(args, Do.RootDirectory, Do.ScriptsDirectory, new DotNetClient());

    internal static async Task<int> Run(
        string[] args,
        AbsolutePath root,
        AbsolutePath scripts,
        IUpdateClient client)
    {
        if (!TryParseArgs(args, out var options))
            return Fail(Usage);

        try
        {
            var manifest = root / ".config/dotnet-tools.json";
            var toolChange = await UpdateDotNetDo(options, manifest, root, client);
            
            var pins = SelectPins(options, scripts);
            var changes = await PrepareChanges(pins, options.Prerelease, root, client);
            ApplyChanges(changes);
            
            Report(changes, toolChange, root);

            return 0;
        }
        catch (Exception exception) when (exception
            is UpdateException
            or IOException
            or UnauthorizedAccessException
            or JsonException
            or InvalidOperationException)
        {
            return Fail(exception.Message);
        }
    }

    static async Task<ToolChange?> UpdateDotNetDo(
        Options options,
        AbsolutePath manifest,
        AbsolutePath root,
        IUpdateClient client)
    {
        if (options.Package is not null)
            return null;

        if (ToolManifest.Contains(manifest, DotNetDoPackage))
            return await client.UpdateTool(DotNetDoPackage, manifest, options.Prerelease, root);

        Console.WriteLine(manifest.IsExistingFile
            ? $"Skipped {DotNetDoPackage}: not declared in {manifest}."
            : $"Skipped {DotNetDoPackage}: no root-local tool manifest.");
        
        return null;
    }

    static Pin[] SelectPins(Options options, AbsolutePath scripts)
    {
        var pins = ReadPins(scripts)
            .Where(pin => options.All
                || string.Equals(pin.Package, options.Package ?? CorePackage, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return options.Package is not null && pins.None()
            ? throw new UpdateException($"No pinned package '{options.Package}' found in scripts.")
            : pins;
    }

    static async Task<PinChange[]> PrepareChanges(Pin[] pins, bool prerelease, AbsolutePath root, IUpdateClient client)
    {
        var latest = new Dictionary<string, NuGetVersion>(StringComparer.OrdinalIgnoreCase);
        foreach (var package in pins.Select(pin => pin.Package).Distinct(StringComparer.OrdinalIgnoreCase))
            latest[package] = await client.FindLatest(package, prerelease, root);

        return pins
            .Select(pin => pin.UpdateTo(latest[pin.Package]))
            .WhereNotNull()
            .ToArray();
    }

    static void ApplyChanges(PinChange[] changes)
    {
        foreach (var group in changes.GroupBy(change => change.Path))
        {
            var text = group.First().Text;
            foreach (var change in group.OrderByDescending(change => change.Start))
                text = text.Remove(change.Start, change.Length).Insert(change.Start, change.Version);
            group.Key.WriteText(text);
        }
    }

    static void Report(PinChange[] changes, ToolChange? toolChange, AbsolutePath root)
    {
        if (toolChange is not null)
            Console.WriteLine($"{toolChange.Package} {toolChange.Previous} -> {toolChange.Version} (tool manifest)");
        
        foreach (var change in changes)
            Console.WriteLine($"{change.Package} {change.Previous} -> {change.Version} ({root.RelativePathTo(change.Path)})");

        if (toolChange is null && changes.None())
            Console.WriteLine("Everything is current.");
    }

    static bool TryParseArgs(string[] args, out Options options)
    {
        options = default;
        string? package = null;
        var all = false;
        var prerelease = false;

        foreach (var arg in args.Skip(1))
        {
            switch (arg)
            {
                case "--all" when !all && package is null:
                    all = true;
                    break;
                case "--prerelease" when !prerelease:
                    prerelease = true;
                    break;
                case not null when !arg.StartsWith('-') && package is null && !all:
                    package = arg;
                    break;
                default:
                    return false;
            }
        }

        options = new(package, all, prerelease);
        return true;
    }

    static Pin[] ReadPins(AbsolutePath scripts)
    {
        if (!scripts.IsExistingDirectory)
            return [];

        return scripts.GlobFiles("*.cs")
            .SelectMany(path =>
                {
                    var text = path.ReadText();
                    return PackageDirective().Matches(text)
                        .Select(match => new Pin(
                            path,
                            text,
                            match.Groups["package"].Value,
                            match.Groups["version"].Value,
                            match.Groups["version"].Index,
                            match.Groups["version"].Length))
                        .Where(pin => NuGetVersion.TryParse(pin.Version, out _));
                })
            .ToArray();
    }

    static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 1;
    }

    [GeneratedRegex(@"(?m)^#:package[ \t]+(?<package>[A-Za-z0-9_.-]+)@(?<version>\S+)[ \t]*$")]
    private static partial Regex PackageDirective();

    readonly record struct Options(string? Package, bool All, bool Prerelease);

    sealed record Pin(AbsolutePath Path, string Text, string Package, string Version, int Start, int Length)
    {
        public PinChange? UpdateTo(NuGetVersion latest)
        {
            var current = NuGetVersion.Parse(Version);
            return latest > current
                ? new(Path, Text, Package, Version, latest.ToNormalizedString(), Start, Length)
                : null;
        }
    }

    sealed record PinChange(
        AbsolutePath Path,
        string Text,
        string Package,
        string Previous,
        string Version,
        int Start,
        int Length);
}

interface IUpdateClient
{
    Task<NuGetVersion> FindLatest(string package, bool prerelease, AbsolutePath root);
    Task<ToolChange?> UpdateTool(string package, AbsolutePath manifest, bool prerelease, AbsolutePath root);
}

sealed record ToolChange(string Package, string Previous, string Version);

sealed class DotNetClient : IUpdateClient
{
    public async Task<NuGetVersion> FindLatest(string package, bool prerelease, AbsolutePath root)
    {
        var result = await Execute(DotNet.PackageSearch with
            {
                SearchTerm = package,
                ExactMatch = true,
                Prerelease = prerelease,
                WorkingDirectory = root,
                Log = IgnoreOutput,
            });
        var versions = result.Sources
            .SelectMany(source => source.Packages)
            .Where(item => string.Equals(item.Id, package, StringComparison.OrdinalIgnoreCase))
            .Select(item => item.Version ?? item.LatestVersion)
            .WhereNotNull()
            .Select(NuGetVersion.Parse)
            .ToArray();

        return versions.None()
            ? throw new UpdateException($"Package '{package}' was not found in the configured sources.")
            : versions.Max()!;
    }

    public async Task<ToolChange?> UpdateTool(string package, AbsolutePath manifest, bool prerelease, AbsolutePath root)
    {
        var previous = ToolManifest.VersionOf(manifest, package);
        await Execute(DotNet.ToolUpdate with
            {
                Package = package,
                ToolManifest = manifest,
                Prerelease = prerelease,
                WorkingDirectory = root,
                Log = IgnoreOutput,
            });
        var version = ToolManifest.VersionOf(manifest, package);
        return previous == version ? null : new(package, previous, version);
    }

    static async Task<T> Execute<T>(ToolCommand<T> command)
    {
        try
        {
            return await command;
        }
        catch (ExecFailedException exception)
        {
            var error = exception.Result.ErrorLines().JoinLines();
            throw new UpdateException(error.IsNullOrWhiteSpace() ? exception.Message : error, exception);
        }
        catch (ToolOutputException exception)
        {
            throw new UpdateException(exception.Message, exception);
        }
    }

    static void IgnoreOutput(OutputType _, string __) { }
}

sealed record ToolManifest
{
    [JsonPropertyName("tools")]
    public Dictionary<string, ToolManifestEntry>? Tools { get; init; }

    public static bool Contains(AbsolutePath path, string package) =>
        path.IsExistingFile
        && Read(path).Tools!.Keys
            .Any(name => string.Equals(name, package, StringComparison.OrdinalIgnoreCase));

    public static string VersionOf(AbsolutePath path, string package)
    {
        var tool = Read(path).Tools!
            .Single(item => string.Equals(item.Key, package, StringComparison.OrdinalIgnoreCase));
        return tool.Value.Version
            ?? throw new UpdateException($"Tool manifest '{path}' has no version for '{package}'.");
    }

    static ToolManifest Read(AbsolutePath path)
    {
        try
        {
            var manifest = path.ReadJson<ToolManifest>();
            return manifest?.Tools is not null
                ? manifest
                : throw new JsonException("The manifest has no 'tools' object.");
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException)
        {
            throw new UpdateException($"Tool manifest '{path}' is invalid.", exception);
        }
    }
}

sealed record ToolManifestEntry
{
    [JsonPropertyName("version")]
    public string? Version { get; init; }
}

sealed class UpdateException(string message, Exception? innerException = null)
    : Exception(message, innerException);
