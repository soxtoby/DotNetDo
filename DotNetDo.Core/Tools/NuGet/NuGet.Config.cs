namespace DotNetDo;
/// <summary>Sets NuGet configuration values or reads one as a path.</summary>
public sealed record NuGetConfig : NuGetConfiguredCommand
{
    /// <summary>Configuration assignments. A null value removes the setting.</summary>
    public IReadOnlyDictionary<string, string?> Settings { get; init => field = Snapshot(value); } = new Dictionary<string, string?>().AsReadOnly();
    /// <summary>The configuration key whose value is returned as an absolute path.</summary>
    public string? PathSettingName { get; init; }
    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts
    {
        get
        {
            if (Settings.Count == 0 && string.IsNullOrWhiteSpace(PathSettingName)) throw new InvalidOperationException("Settings or PathSettingName is required.");
            return
                [
                    "nuget config",
                    Args("-Set", Settings.Select(x => $"{x.Key}={x.Value ?? ""}"), " -Set "),
                    Arg("-AsPath", PathSettingName),
                    .. ConfiguredParts,
                ];
        }
    }
}
