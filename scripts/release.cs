#!/usr/bin/env dotnet
#:package DotNetDo.Core@0.1.0
using System.Text.RegularExpressions;
using DotNetDo;

var tag = Do.Param("tag", Environment.GetEnvironmentVariable("GITHUB_REF_NAME"), "Release tag.").Required().Value;
var apiKey = Do.Secret("nuget_api_key", null, "Temporary NuGet API key.").Required().Unwrap();
var project = (Do.RootDirectory / "Directory.Build.props").ReadText();
var versionMatch = Regex.Match(project, @"<VersionPrefix>(?<version>[^<]+)</VersionPrefix>");
if (!versionMatch.Success)
    throw new InvalidOperationException("Directory.Build.props has no VersionPrefix.");

var expectedTag = "v" + versionMatch.Groups["version"].Value;
if (!string.Equals(tag, expectedTag, StringComparison.Ordinal))
    throw new InvalidOperationException($"Tag '{tag}' does not match project version '{expectedTag}'.");

var changelog = (Do.RootDirectory / "CHANGELOG.md").ReadText().Replace("\r\n", "\n");
var notesMatch = Regex.Match(
    changelog,
    $@"(?ms)^## {Regex.Escape(tag)}\s*\n(?<notes>.*?)(?=^## |\z)");
if (!notesMatch.Success || string.IsNullOrWhiteSpace(notesMatch.Groups["notes"].Value))
    throw new InvalidOperationException($"CHANGELOG.md has no release notes for {tag}.");

var packages = Do.RootDirectory / "artifacts" / "packages";
var packageFiles = Directory.GetFiles(packages, "*.nupkg")
    .ToArray();
var symbolFiles = Directory.GetFiles(packages, "*.snupkg");
var releaseFiles = packageFiles.Concat(symbolFiles).ToArray();
if (packageFiles.Length != 2 || symbolFiles.Length != 2)
    throw new InvalidOperationException("Expected two NuGet packages and two symbol packages.");

foreach (var package in packageFiles)
{
    await Do.Exec($"dotnet nuget push {package.QuotedArgument()} --api-key {apiKey.QuotedArgument()} --source https://api.nuget.org/v3/index.json --skip-duplicate");
}

var notesFile = Do.RootDirectory / "artifacts" / "release-notes.md";
notesFile.WriteText(notesMatch.Groups["notes"].Value.Trim() + Environment.NewLine);
var assets = string.Join(" ", releaseFiles.Select(path => path.QuotedArgument()));
await Do.Exec($"gh release create {tag.QuotedArgument()} {assets} --title {tag.QuotedArgument()} --notes-file {notesFile.QuotedArgument()}");
