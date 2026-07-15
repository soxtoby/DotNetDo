# Tool and Core use separate packages

The `DotNetDo` package remains the .NET tool so pipelines read naturally as `dnx DotNetDo <task>`, while `DotNetDo.Core` contains the `DotNetDo` namespace consumed by file-based apps. NuGet rejects a package typed as a .NET tool when used as a normal package reference, making the original single-package design impossible; both packages therefore share one version and are released together.
