#!/usr/bin/env dotnet
#:package DotNetDo.Core@0.7.0
using DotNetDo;
using static DotNetDo.Tools;

[assembly: TaskDescription("Build and optionally serve the documentation site.")]

var serve = Do.Param("serve", false, "Serve the generated site on http://localhost:8080.").Value;
var siteOnly = Do.Param("site-only", false, "Reuse the previously generated API data instead of regenerating it from the built DotNetDo.Core.").Value;
var root = Do.RootDirectory;
var docfxConfig = root / "docs" / "docfx.json";
var familyConfig = root / "docs" / "reference" / "families.json";
var composerProject = root / "docs" / "tools" / "DocComposer" / "DocComposer.csproj";
var rawApi = root / "artifacts" / "docs" / "api-raw";
var composedApi = root / "artifacts" / "docs" / "composed";
var site = root / "artifacts" / "docs" / "site";

var coreAssembly = root / "DotNetDo.Core" / "bin" / "Release" / "net10.0" / "DotNetDo.Core.dll";

if (siteOnly && !rawApi.IsExistingDirectory)
    throw new InvalidOperationException($"No generated API data in {rawApi}. Run the task without --site-only first.");
if (!siteOnly && !coreAssembly.IsExistingFile)
    throw new InvalidOperationException($"No Release build at {coreAssembly}. Run the build task first.");

await (DotNet.ToolRestore with { WorkingDirectory = root });

// Reads the Release build of DotNetDo.Core rather than producing it, so building the solution stays the build task's job.
if (!siteOnly)
    await new DotNetInvocation(["docfx", "metadata", docfxConfig]) { WorkingDirectory = root };

await (DotNet.Build with
    {
        Targets = [composerProject],
        Configuration = "Release",
        WorkingDirectory = root,
    });
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
