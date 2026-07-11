# AbsolutePath owns filesystem operations

`AbsolutePath` directly exposes explicitly named filesystem queries and commands, while construction, normalization, and identity remain lexical and deterministic. This favors discoverability and a compact authored API over a separate filesystem service; `RelativePath` has no filesystem operations because resolving it implicitly would introduce ambient working-directory state. Operations pass the path's native rendering to `System.IO` and retain its familiar platform behavior.
