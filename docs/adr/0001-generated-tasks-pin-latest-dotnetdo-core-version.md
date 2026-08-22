# Generated tasks pin the latest stable DotNetDo.Core version

`:init` and `:new` query the workspace's configured NuGet sources for the latest stable `DotNetDo.Core` version and write that exact version into each generated task. The CLI version may be older than the latest published Core package or may identify an unpublished local build, so it does not determine the generated package reference.

Task creation fails when the package search fails or no configured source contains `DotNetDo.Core`. Existing files remain untouched. A successful exact pin gives task restores a repeatable input and lets `:update` advance generated tasks later.
