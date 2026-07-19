using Microsoft.Build.Locator;

namespace DotNetDo;

/// <summary>Locates commands supplied by the newest installed MSBuild toolset.</summary>
static class MSBuildToolset
{
    static readonly Lazy<string> MSBuildCommand = new(LocateMSBuild);
    static readonly Lazy<string> VSTestCommand = new(LocateVSTest);
    static readonly Lazy<VisualStudioInstance> Instance = new(LocateInstance);

    public static string MSBuild => MSBuildCommand.Value;
    public static string VSTest => VSTestCommand.Value;

    static VisualStudioInstance LocateInstance() =>
        MSBuildLocator.QueryVisualStudioInstances()
            .OrderByDescending(candidate => candidate.Version)
            .FirstOrDefault() ?? throw new InvalidOperationException("No MSBuild installation was found.");

    static string LocateMSBuild() =>
        Resolve(AbsolutePath.Parse(Instance.Value.MSBuildPath), "MSBuild")
        ?? throw new FileNotFoundException($"MSBuild was not found under '{Instance.Value.MSBuildPath}'.");

    static string LocateVSTest()
    {
        var instance = Instance.Value;
        var command = Resolve(AbsolutePath.Parse(instance.MSBuildPath), "vstest.console");
        if (command is not null)
            return command;

        if (!string.IsNullOrWhiteSpace(instance.VisualStudioRootPath))
        {
            var directory = AbsolutePath.Parse(instance.VisualStudioRootPath) / "Common7" / "IDE" / "CommonExtensions" / "Microsoft" / "TestWindow";
            command = Resolve(directory, "vstest.console");
            if (command is not null)
                return command;
        }

        throw new FileNotFoundException($"VSTest was not found for MSBuild installation '{instance.Name}'.");
    }

    static string? Resolve(AbsolutePath directory, string name)
    {
        var executable = directory / $"{name}.exe";
        if (executable.IsExistingFile)
            return executable.QuotedArgument();

        var assembly = directory / $"{name}.dll";
        if (assembly.IsExistingFile)
            return $"dotnet {assembly.QuotedArgument()}";

        return null;
    }
}
