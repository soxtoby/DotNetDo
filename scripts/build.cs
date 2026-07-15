#!/usr/bin/env dotnet
#:package DotNetDo.Core@0.1.0-bootstrap.1
using DotNetDo;

var packages = Do.RootDirectory / "artifacts" / "packages";
if (packages.IsExistingDirectory)
    packages.Delete();
packages.EnsureDirectoryExists();

await Tools.DotNet.Restore;
await (Tools.DotNet.Build with
{
    Configuration = "Release",
    NoRestore = true,
});
await (Tools.DotNet.Test with
{
    Configuration = "Release",
    NoBuild = true,
    NoRestore = true,
});
await (Tools.DotNet.Pack with
{
    Targets = [(Do.RootDirectory / "DotNetDo.Core" / "DotNetDo.Core.csproj").QuotedArgument()],
    Configuration = "Release",
    NoBuild = true,
    NoRestore = true,
    Output = packages.QuotedArgument(),
});
await (Tools.DotNet.Pack with
{
    Targets = [(Do.RootDirectory / "DotNetDo" / "DotNetDo.csproj").QuotedArgument()],
    Configuration = "Release",
    NoBuild = true,
    NoRestore = true,
    Output = packages.QuotedArgument(),
});
