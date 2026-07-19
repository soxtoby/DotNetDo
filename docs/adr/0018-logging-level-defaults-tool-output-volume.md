# Logging Level Defaults Tool Output Volume

Fresh tool commands snapshot `Logging.Level` as a best-effort default for each dedicated native output-volume control. Explicit typed values override only their own control; clearing a materialized default restores the tool's native behavior. Raw `AdditionalArguments` remain opaque. Replacing Serilog's global logger does not replace this DotNetDo preference.

Five-level controls map `Verbose` to diagnostic, `Debug` to detailed, `Information` to normal, `Warning` to minimal, and both `Error` and `Fatal` to quiet. Quiet/verbose pairs map `Verbose` and `Debug` to verbose, `Information` to neither, and `Warning` or higher to quiet. One-sided controls use their applicable half of that mapping.

Mappings may be shared within a tool family but not through a universal public verbosity abstraction. Compound logger configuration, diagnostic files, binary logs, and other behavior-changing diagnostics are not volume controls. A command with multiple independent controls receives a default for each; overriding one does not suppress the others. Internal package-tool restore derives its own defaults rather than inheriting the requested command's overrides.

This policy controls generated output volume only. Exec continues to capture all output and log stdout as `Information` and stderr as `Error`; DotNetDo does not parse tool text to infer event severity.
