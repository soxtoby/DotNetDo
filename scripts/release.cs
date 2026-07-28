#!/usr/bin/env dotnet
#:package DotNetDo.Core@0.4.0
using System.Text.RegularExpressions;
using DotNetDo;
using static DotNetDo.Tools;

var tag = Do.GitHubActions?.Workflow.ReferenceName is { } defaultTag
    ? Do.Param("tag", defaultTag, "Release tag.").Value
    : Do.Param("tag").Required().Value;
var apiKey = Do.Secret("nuget_api_key", null, "Temporary NuGet API key.").Required();

var project = (Do.RootDirectory / "Directory.Build.props").ReadText();
var versionMatch = project.RegexMatch("<VersionPrefix>(?<version>[^<]+)</VersionPrefix>");
if (!versionMatch.Success)
    throw new InvalidOperationException("Directory.Build.props has no VersionPrefix.");

var expectedTag = "v" + versionMatch.Groups["version"].Value;
if (tag != expectedTag)
    throw new InvalidOperationException($"Tag '{tag}' does not match project version '{expectedTag}'.");

var changelog = (Do.RootDirectory / "CHANGELOG.md").ReadText().Replace("\r\n", "\n");
var notesMatch = changelog.RegexMatch($@"(?ms)^## {Regex.Escape(tag)}\s*\n(?<notes>.*?)(?=^## |\z)");
if (!notesMatch.Success || notesMatch.Groups["notes"].Value.IsNullOrWhiteSpace())
    throw new InvalidOperationException($"CHANGELOG.md has no release notes for {tag}.");

var packages = Do.RootDirectory / "artifacts" / "packages";
var packageFiles = packages.GlobFiles("*.nupkg");
var symbolFiles = packages.GlobFiles("*.snupkg");
var releaseFiles = packageFiles.Concat(symbolFiles).ToArray();
if (packageFiles.Length != 2 || symbolFiles.Length != 2)
    throw new InvalidOperationException("Expected two NuGet packages and two symbol packages.");

foreach (var package in packageFiles)
{
    await (DotNet.NuGetPush with
        {
            Package = package,
            ApiKey = apiKey.Unwrap(),
            Source = "https://api.nuget.org/v3/index.json",
            SkipDuplicate = true,
        });
}

var notesFile = Do.RootDirectory / "artifacts" / "release-notes.md";
notesFile.WriteText(notesMatch.Groups["notes"].Value.Trim() + Environment.NewLine);
var assets = releaseFiles.Select(path => path.QuotedArgument()).JoinWith(" ");
await Do.Exec($"gh release create {tag.QuotedArgument()} {assets} --title {tag.QuotedArgument()} --notes-file {notesFile.QuotedArgument()}");
