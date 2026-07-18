---
name: generate-tool-command
description: Generate a typed DotNetDo tool command and its Tools entry.
disable-model-invocation: true
---

# Generate a tool command

## 1. Ground the seam

Read the current DotNetDo.Core/ToolCommand.cs, DotNetDo.Core/PackageToolCommand.cs, the closest files under DotNetDo.Core/Tools/, their tests, and applicable ADRs. Treat them as authoritative over remembered patterns.

Complete when the current command hierarchy, rendering helpers, Tools shape, and test conventions are identified.

## 2. Classify ownership

Choose the execution branch before designing the API:

| Source | Base | Prefix ownership | Lifecycle |
| --- | --- | --- | --- |
| Executable supplied on PATH | ExecToolCommand, or ToolCommand<TResult> for parsed output | Command record supplies the executable and subcommand | Host environment owns installation and version |
| .NET local tool supplied by a NuGet package | PackageToolCommand<TResult> | Base renders dotnet tool run; derived constructor supplies exact package ID and command name | Local tool manifest owns declaration/version; package infrastructure owns validation, restore, and retry |

Use ExecResult when callers need raw successful output. Use a semantic result type only when the tool has a stable machine-readable contract. If a tool supports both distribution models, follow the requested or repository-owned model; ask one focused question when that choice remains material and unresolved.

Complete when the source category, executable or package identity, command name, and result type are explicit.

## 3. Capture the CLI contract

Run the installed tool's help for a PATH executable; use authoritative CLI documentation when it is unavailable. For a package tool, verify the package ID and exposed command from authoritative package documentation, then use its help when runnable. Record exact option spelling, positional ordering, repeatable values, mutually exclusive choices, defaults, and output format.

Complete when every implemented property and forced argument traces to the verified contract.

## 4. Implement the authored shape

Add the command beside its peers under DotNetDo.Core/Tools/.

- Extend the existing public static partial class Tools.
- Use a nested static tool class for a command suite, such as Tools.DotNet.Build; use a direct getter for a single command, such as Tools.GitVersion.
- Return new() from every default getter so each access is a fresh record.
- Model commands as sealed records. Introduce a base record only for genuinely shared CLI semantics.
- For package tools, pass the package ID and exposed command to PackageToolCommand<TResult> and let its prefix/lifecycle stand. Keep versions in .config/dotnet-tools.json, adding a manifest entry only when this repository must execute the package.
- Store structured options with SetArgument, SetArgumentArray, SetFlag, and SetEnum; read them through the matching getters.
- Pass semantic values unquoted. Rendering quotes them. Use quote: false for intentional raw CLI fragments and AdditionalArguments as the raw escape hatch.
- Put stable forced arguments in the constructor. Keep result conversion in ReadResult; the generic command boundary owns conversion error wrapping.
- Document behavior, ownership, constraints, and gotchas in XML comments.

Complete when the public usage is terse, with-customizable, awaitable, and follows the selected ownership branch without duplicating lifecycle policy.

## 5. Lock behavior

Add focused tests covering the default render, every argument shape, values containing spaces, with replacement, and validation constraints. For a package tool, assert package ID, exposed command, and dotnet tool run rendering. For a typed result, test representative parsing and preservation of useful unknown fields when the format is extensible.

Update CHANGELOG.md. Run the focused tests, then the full test suite and build. Inspect the final diff for handwritten quoting, cached default records, embedded package versions, and unrelated changes.

Complete only when tests and build pass and every changed public member is covered by behavior-focused documentation.
