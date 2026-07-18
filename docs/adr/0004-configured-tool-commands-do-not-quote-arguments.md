---
status: superseded by ADR-0017
---

# Configured Tool Commands Do Not Quote Arguments

Configured tool helpers build command text for `Do.Exec` by concatenating the configured executable, fixed arguments, positional arguments, named arguments, flags, and additional arguments.

Structured argument values are emitted as provided. DotNetDo does not automatically quote or escape them.

Callers or concrete helpers that need quoting should opt in with a helper extension method. This keeps the configured-tool layer predictable, preserves raw CLI syntax, and avoids hiding command-line parsing rules inside generic configuration code.
