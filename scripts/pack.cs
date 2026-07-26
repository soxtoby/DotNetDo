#!/usr/bin/env dotnet
#:package DotNetDo.Core@0.4.0
using DotNetDo;
using static DotNetDo.Tools;

var packages = (Do.RootDirectory / "artifacts" / "packages").RecreateDirectory();

await (DotNet.Pack with
    {
        Targets = [Do.Solution["DotNetDo.Core"]],
        Output = packages,
    });

await (DotNet.Pack with
    {
        Targets = [Do.Solution["DotNetDo"]],
        Output = packages,
    });
