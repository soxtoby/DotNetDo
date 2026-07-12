# Git

DotNetDo provides build-focused Git repository information and a small set of common mutations. LibGit2Sharp owns discovery and reads. The Git executable owns writes.

## Repository

`new GitRepository(AbsolutePath directory)` discovers the working-tree repository containing `directory` and fails when none exists. Bare repositories and file paths are unsupported. The instance is permanently bound to the discovered root and owns its exposed LibGit2Sharp `Repository` until disposed.

`Do.GitRepo` synchronously and lazily discovers from `Environment.CurrentDirectory`. It may be replaced. DotNetDo disposes only the instance it created; replacing it disposes neither value.

## Live information

Only `Root` is stable. Every other read reflects current repository state.

- `Root` is an `AbsolutePath`.
- `CurrentBranch` is the friendly branch name, or `null` for detached HEAD.
- `CurrentCommit` is a LibGit2Sharp `Commit` and throws when the repository has no commit.
- `Changes` adapts each LibGit2Sharp status entry into a `GitChange` containing a root-relative `RelativePath` and the unchanged LibGit2Sharp `FileStatus`.
- `IsDirty` is equivalent to `Changes.Count != 0`.
- `Tags` exposes LibGit2Sharp's live tag collection.
- `Repository` exposes the underlying LibGit2Sharp repository for deeper operations.

`CommitsSince(Branch base)` finds the merge base of `HEAD` and `base`, then walks first parents from `HEAD`, newest first, excluding the merge base. It excludes commits reachable only through merged side branches. `CommitsSince(string base)` resolves only an exact local branch such as `main` or exact remote-tracking branch such as `origin/main`; it does not search remotes by short name. Missing branches, unrelated histories, and an unborn `HEAD` fail.

## Commands

Repository command values are awaitable and invoke Git through `Do.Exec` as `git -C <root> ...`. Concrete helpers apply `QuotedArgument()` to values; raw Git syntax remains raw. All path inputs are repository-root-relative `RelativePath` values and must not escape the root. Git owns Git-specific configuration, hooks, signing, credentials, validation, and failures.

Commands are available both from a bound repository (`repo.Add`) and as default values under `Tools.Git` (`Tools.Git.Add`). `Tools.Git` resolves `Do.GitRepo` only when the command is rendered or awaited.

- `Add`: stages whole files. Exactly one of `Paths` or `All` is required. `All` maps to `git add --all`.
- `Reset`: unstages whole files without moving `HEAD` or changing working files. Exactly one of `Paths` or `All` is required.
- `Commit`: requires `Message`; `All` includes modified and deleted tracked files but not untracked files. Optional `GitAuthor` overrides name and email; Git supplies the timestamp. Amend and hook bypass are unsupported.
- `Push`: performs a normal push, optionally to a supplied `Remote`. It does not set upstream and exposes no force, mirror, deletion, all-branch, or arbitrary-refspec options.
- `CreateTag`: creates a local annotated tag with required `Name` and `Message`; optional LibGit2Sharp `Commit Target` defaults to `CurrentCommit`.
- `PushTag`: pushes one required LibGit2Sharp `Tag`, optionally to a supplied `Remote`. Without one, DotNetDo resolves Git's configured default push destination. Git reports invalid remotes or tags.

`GitRepository.Exec(string arguments, ExecOptions? options = null)` runs raw Git arguments against the bound repository and preserves normal Exec behavior. `ExecOptions.WorkingDirectory` remains allowed because `-C` independently fixes Git's repository context.

## Excluded

Merge, rebase, checkout, pull, fetch, stash, tag deletion or movement, history-moving reset, chunk staging, conflict handling, and advanced push behavior are outside the typed convenience API. Use the exposed LibGit2Sharp repository or repository-bound `Exec` when needed.
