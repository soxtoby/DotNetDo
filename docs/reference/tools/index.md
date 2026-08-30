---
uid: DotNetDo.Tools
---

# Tool APIs

Typed immutable commands model external command-line tools. Configure a command with a record `with` expression, then await it.

```csharp
await Tools.DotNet.Build with
{
    Configuration = "Release",
};
```

- [Azure and Bicep](azure.yml)
- [Bun](bun.yml)
- [.NET SDK](dotnet.yml)
- [Git](git.yml)
- [GitVersion](gitversion.yml)
- [MSBuild](msbuild.yml)
- [npm](npm.yml)
- [NuGet CLI](nuget.yml)
- [Scoop](scoop.yml)
- [VSTest](vstest.yml)
