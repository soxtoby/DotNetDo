# Meta-tasks are configured task sequences

DotNetDo represents common orchestration scenarios as meta-tasks in the `tasks` table of `dotnetdo.toml`, using command-like strings instead of C# task files or inferred prerequisite state. Meta-tasks are static, ordered, fail-fast sequences of task invocations: this keeps the safe default workflow easy to run while letting local and CI callers select narrower component tasks when prior work should be reused. DotNetDo deliberately does not infer freshness, cache or deduplicate executions, or synthesize a dependency graph.
