# Exec Helper Is Text-Oriented And Awaitable

DotNetDo's `Do.Exec` helper runs external commands from file-based apps and favors script ergonomics over full process-control coverage. It accepts one command string, parses only the program token, passes the remaining argument string to .NET process execution, captures stdout and stderr as replayable text lines and decoded text chunks, buffers completed text/results in memory, and makes `await Do.Exec(...)` equivalent to awaiting `Succeeded`, while `Completed` returns the result without treating non-zero exit codes as exceptions.
