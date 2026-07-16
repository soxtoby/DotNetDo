# Exec Helper Is Text-Oriented And Awaitable

DotNetDo's `Do.Exec` helper runs external commands from tasks and favors task ergonomics over full process-control coverage. It accepts one command string, parses only the program token, passes the remaining argument string to .NET process execution, captures stdout and stderr as replayable text lines, buffers completed results in memory, and makes `await Do.Exec(...)` equivalent to awaiting `Succeeded`, while `Completed` returns the result without treating non-zero exit codes as exceptions.

`ExecResult.AllOutput` preserves captured stdout and stderr together as typed lines in observed order. `OutputLines()` and `ErrorLines()` return only the messages from their respective streams. `ReadText()` joins standard-output lines with the current environment's newline and adds no trailing newline; original line endings and final-newline presence are not recoverable. Structured readers consume this reconstructed standard-output text: `ReadJson<T>()`, `ReadToml<T>()`, `ReadYaml<T>()`, and `ReadXml<T>()`. No separate `ReadLines()` duplicates `OutputLines()`.

The structured readers mirror the corresponding `AbsolutePath` reader options: JSON accepts `JsonSerializerOptions`, TOML accepts `TomlSerializerOptions`, YAML accepts a YamlDotNet `IDeserializer`, and XML has no options. They accept no encoding because process output has already been decoded.

Readers do not inspect `ExitCode`; they parse any captured result. Success enforcement belongs to choosing `Succeeded` or `Completed` when obtaining the result, not to reading that result.

`OutputLines()` and `ErrorLines()` preserve observed order within the selected stream and allocate a fresh string array on every call. Mutating that array does not mutate `AllOutput`.

Structured readers expose the underlying serializer's normal result and exceptions, including for empty or malformed standard output. They add no Exec-specific parse wrapper.

When a typed `ToolCommand<TResult>` converts its successful `ExecResult`, the command boundary wraps any conversion exception in `ToolOutputException`. The wrapper retains the raw result and expected result type, so individual typed commands need no repeated error-handling policy. Calling an `ExecResult` reader directly still exposes its serializer's native exceptions.

The existing `ExecResult.Output` member is renamed directly to `AllOutput`; no obsolete compatibility alias remains. The explicit name distinguishes the combined typed stream from the standard-output-only helpers.
