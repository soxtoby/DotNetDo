# MSBuild evaluations have process lifetime

`ProjectInfo` exposes a cached default evaluated project and returns raw `Microsoft.Build.Evaluation.Project` instances for custom global properties. Evaluations use MSBuild's global project collection, so matching project paths and global properties share one mutable instance retained for the process lifetime. This removes caller-owned disposal and makes evaluated projects work naturally in collections, trading away isolated evaluations and deterministic unloading; DotNetDo therefore exposes no unload or reset operation.
