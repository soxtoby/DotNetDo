#!/usr/bin/env dotnet
#:package DotNetDo.Core@*
using DotNetDo;
using static DotNetDo.Tools;

var packages = Do.RootDirectory / "artifacts" / "packages";
if (packages.IsExistingDirectory)
    packages.Delete();
packages.EnsureDirectoryExists();

await (DotNet.Pack with
    {
        Targets = [Do.Solution["DotNetDo.Core"].Path],
        Output = packages,
    });

await (DotNet.Pack with
    {
        Targets = [Do.Solution["DotNetDo"].Path],
        Output = packages,
    });