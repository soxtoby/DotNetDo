namespace DotNetDo;

/// <summary>Undoes MSBuildLocator's process-wide environment changes for child processes.</summary>
/// <remarks>
/// MSBuildLocator points MSBuild environment variables at the SDK it registers, which is limited to SDKs the current
/// runtime can load. A child <c>dotnet</c> process may resolve a newer SDK, which fails when it inherits those variables.
/// </remarks>
static class MSBuildEnvironment
{
    /// <summary>The AppContext data name under which the generated MSBuild registration stores the inherited values.</summary>
    const string InheritedDataName = "DotNetDo.InheritedMSBuildEnvironment";

    public static void RestoreInherited(IDictionary<string, string?> environment)
    {
        if (AppContext.GetData(InheritedDataName) is not IReadOnlyDictionary<string, string?> inherited)
            return;

        foreach (var (name, value) in inherited)
        {
            if (value is null)
                environment.Remove(name);
            else
                environment[name] = value;
        }
    }
}
