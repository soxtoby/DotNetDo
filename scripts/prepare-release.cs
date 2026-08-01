#!/usr/bin/env dotnet
#:package DotNetDo.Core@0.5.0
using System.Text.RegularExpressions;
using DotNetDo;
using Serilog;

if (Do.GitRepo.IsDirty)
    throw new InvalidOperationException("prepare-release requires a clean Git worktree.");

var projectFile = Do.RootDirectory / "Directory.Build.props";
var changelogFile = Do.RootDirectory / "CHANGELOG.md";
var manifestFile = Do.RootDirectory / ".config" / "dotnet-tools.json";
var project = projectFile.ReadText();
var changelog = changelogFile.ReadText().Replace("\r\n", "\n");

var current = ParseProjectVersion(project);
var latest = ParseLatestRelease(changelog);
if (current != new Version(0, 0, 0) && latest != current)
    throw new InvalidOperationException($"Project version {current} does not match latest changelog release {latest?.ToString() ?? "<none>"}.");

var unreleasedMatch = ParseUnreleased(changelog);
var unreleased = unreleasedMatch.Groups["notes"].Value;
var bump = InferBump(unreleased);
var next = bump switch
    {
        Bump.Major => new Version(current.Major + 1, 0, 0),
        Bump.Minor => new Version(current.Major, current.Minor + 1, 0),
        _ => new Version(current.Major, current.Minor, current.Build + 1),
    };

if (current != new Version(0, 0, 0))
    UpdatePins(current.ToString(), manifestFile);

projectFile.WriteText(project.RegexReplace(
    "<VersionPrefix>[^<]+</VersionPrefix>",
    $"<VersionPrefix>{next}</VersionPrefix>"));

var before = changelog[..unreleasedMatch.Index];
var after = changelog[(unreleasedMatch.Index + unreleasedMatch.Length)..].TrimStart('\n');
var released = $"## Unreleased\n\n## v{next}\n\n{unreleased.Trim()}\n";
changelogFile.WriteText(before + released + (after.Length == 0 ? "" : "\n" + after.TrimEnd() + "\n"));

Log.Information("Next version: {Next}", next);

static Version ParseProjectVersion(string project)
{
    var match = project.RegexMatch("<VersionPrefix>(?<version>[^<]+)</VersionPrefix>");
    return match.Success && Version.TryParse(match.Groups["version"].Value, out var version)
        ? version
        : throw new InvalidOperationException("Directory.Build.props has no valid VersionPrefix.");
}

static Version? ParseLatestRelease(string changelog)
{
    var match = changelog.RegexMatch(@"(?m)^## v(?<version>\d+\.\d+\.\d+)\s*$");
    return match.Success ? Version.Parse(match.Groups["version"].Value) : null;
}

static Match ParseUnreleased(string changelog)
{
    var match = changelog.RegexMatch(@"(?ms)^## Unreleased\s*\n(?<notes>.*?)(?=^## |\z)");
    if (!match.Success || match.Groups["notes"].Value.IsNullOrWhiteSpace())
        throw new InvalidOperationException("CHANGELOG.md has no Unreleased notes.");
    return match;
}

static Bump InferBump(string notes)
{
    var headings = notes.RegexMatches(@"(?m)^### (?<heading>.+?)\s*$")
        .Select(match => match.Groups["heading"].Value)
        .ToArray();
    if (headings.None())
        throw new InvalidOperationException("Unreleased notes have no change headings.");

    var unknown = headings.Except(["Breaking", "Added", "Changed", "Fixed"], StringComparer.Ordinal).ToArray();
    if (unknown.Any())
        throw new InvalidOperationException($"Unknown Unreleased change heading: {unknown.JoinWith(", ")}.");

    return headings.Contains("Breaking", StringComparer.Ordinal) ? Bump.Major
        : headings.Contains("Added", StringComparer.Ordinal) || headings.Contains("Changed", StringComparer.Ordinal) ? Bump.Minor
        : Bump.Patch;
}

static void UpdatePins(string version, AbsolutePath manifestFile)
{
    var manifest = manifestFile.ReadText();
    var manifestVersion = new Regex("""(?m)("version"\s*:\s*")[^"]+(")""");
    if (!manifestVersion.IsMatch(manifest))
        throw new InvalidOperationException("Tool manifest has no package version.");
    manifestFile.WriteText(manifestVersion.Replace(manifest, $"${{1}}{version}${{2}}", 1));

    foreach (var script in (Do.RootDirectory / "scripts").GlobFiles("*.cs"))
    {
        var content = script.ReadText();
        script.WriteText(Regex.Replace(
            content,
            @"(?m)^(#:package\s+DotNetDo\.Core@)[^\s]+",
            $"${{1}}{version}"));
    }
}

enum Bump { Major, Minor, Patch }