# Tool commands quote structured arguments

Tool-command setters treat values as semantic command-line arguments and quote them by default; concrete commands pass `quote: false` for intentionally raw syntax, while flags and additional arguments remain raw. Callers therefore assign unquoted values, getters return those values unchanged, and `QuotedArgument()` remains available for raw textual command composition.

Argument slots retain scalar and collection values structurally until rendering. Each collection element is one independently quoted argument, caller-owned collections are snapshotted, and composite arguments such as `key=value` are assembled before being quoted once. Setters preserve null, empty, and whitespace values plus their slot positions; rendering alone omits blank values.

Pre-quoted typed-property input receives no compatibility handling. Existing callers must remove `QuotedArgument()` rather than maintaining two ambiguous authored forms.
