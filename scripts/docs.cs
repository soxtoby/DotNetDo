#!/usr/bin/env dotnet
#:package DotNetDo.Core@0.6.0
using DotNetDo;
using static DotNetDo.Tools;

[assembly: TaskDescription("Build and optionally serve the documentation site.")]

var serve = Do.Param<bool>("serve", false, "Serve the generated site on http://localhost:8080.").Value;
var root = Do.RootDirectory;
var docfxConfig = root / "docs" / "docfx.json";
var familyConfig = root / "docs" / "reference" / "families.json";
var composerProject = root / "docs" / "tools" / "DocComposer" / "DocComposer.csproj";
var rawApi = root / "artifacts" / "docs" / "api-raw";
var composedApi = root / "artifacts" / "docs" / "composed";
var site = root / "artifacts" / "docs" / "site";

await (DotNet.ToolRestore with { WorkingDirectory = root });
await (DotNet.Build with
{
    Targets = [root / "DotNetDo.Core" / "DotNetDo.Core.csproj"],
    Configuration = "Release",
    WorkingDirectory = root,
});
await (DotNet.Build with
{
    Targets = [composerProject],
    Configuration = "Release",
    WorkingDirectory = root,
});

await new DotNetInvocation(["docfx", "metadata", docfxConfig]) { WorkingDirectory = root };
await new DotNetInvocation([
    "run",
    "--project", composerProject,
    "--configuration", "Release",
    "--no-build",
    "--no-restore",
    "--",
    rawApi,
    composedApi,
    familyConfig,
]) { WorkingDirectory = root };
await new DotNetInvocation(["docfx", "build", docfxConfig, "--warningsAsErrors"]) { WorkingDirectory = root };

if (serve)
    await new DotNetInvocation(["docfx", "serve", site, "--hostname", "localhost", "--port", "8080"]) { WorkingDirectory = root };

sealed record DotNetInvocation(IReadOnlyList<string> Arguments) : ExecToolCommand
{
    protected override IReadOnlyList<string?> CommandParts => ["dotnet", Args(Arguments)];
}
