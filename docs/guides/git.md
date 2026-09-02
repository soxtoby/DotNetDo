# Git

DotNetDo gives tasks a repository rooted at the workspace. Use it to inspect the current branch and changes, run Git commands, or prove that a formatter or generator left tracked content unchanged.

## Read repository state

`Do.GitRepo` finds the repository containing the DotNetDo root. Its branch, commit, changes, and tags reflect the current repository state.

```csharp
if (Do.GitRepo.IsDirty)
    Log.Warning("Build started with {Count} changed files", Do.GitRepo.Changes.Count);

foreach (var commit in Do.GitRepo.CommitsSince("origin/main"))
    Log.Information("{Sha} {Message}", commit.Sha[..7], commit.MessageShort);
```

Branch names must identify an exact local or remote-tracking branch.

## Verify generated content

Wrap a formatter or generator with `VerifyUnchanged` when CI should fail if committed output is stale:

```csharp
await Do.GitRepo.VerifyUnchanged(async () =>
{
    await Tools.DotNet.Format;
});
```

Pre-existing changes are allowed as long as the operation does not alter them. If files change, DotNetDo leaves them in place and reports their paths.

## Run Git commands

Typed commands cover common task operations. This example commits generated release notes and pushes the current branch:

```csharp
await (Do.GitRepo.Add with { Paths = [new RelativePath("CHANGELOG.md")] });
await (Do.GitRepo.Commit with { Message = "Update release notes" });
await Do.GitRepo.Push;
```

Use `Do.GitRepo.Exec(...)` for Git commands that have no typed helper. Paths passed to repository commands are relative to the repository root.

See the [Git repositories API reference](../reference/core/git-repositories.yml) and [typed Git commands](../reference/tools/git.yml) for command options and return types.
