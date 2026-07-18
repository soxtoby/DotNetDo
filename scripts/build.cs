#!/usr/bin/env dotnet
#:package DotNetDo.Core@0.1.0
using DotNetDo;
using static DotNetDo.Tools;

var configuration = Do.Param("configuration", "Debug");

var packages = Do.RootDirectory / "artifacts" / "packages";
if (packages.IsExistingDirectory)
    packages.Delete();
packages.EnsureDirectoryExists();

await DotNet.Restore;

await (DotNet.Build with
    {
        Configuration = configuration,
        NoRestore = true,
    });

await (DotNet.Test with
    {
        Configuration = configuration,
        NoBuild = true,
        NoRestore = true,
    });

await (DotNet.Pack with
    {
        Targets = [(Do.RootDirectory / "DotNetDo.Core" / "DotNetDo.Core.csproj").ToString()],
        Configuration = configuration,
        NoBuild = true,
        NoRestore = true,
        Output = packages.ToString(),
    });

await (DotNet.Pack with
    {
        Targets = [(Do.RootDirectory / "DotNetDo" / "DotNetDo.csproj").ToString()],
        Configuration = configuration,
        NoBuild = true,
        NoRestore = true,
        Output = packages.ToString(),
    });
