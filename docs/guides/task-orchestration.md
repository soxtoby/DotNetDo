# Task orchestration

DotNetDo configuration may define meta-tasks alongside C# tasks:

```toml
[tasks]
test = ["build", "test-components"]
test-components = ["test-csharp", "test-javascript"]
coverage = "test-csharp --coverage"
```

A string defines one task invocation. A string array defines an ordered sequence. The first token of each string is a task name; the remaining text is passed as fixed arguments.

This supports distinct workflows without prerequisite tracking:

```text
do test                 # build, then both test components
do test-components      # reuse an existing build
do test-csharp          # run one component
```

On CI, run `do build` in the Build step and `do test-components` in the Test step.

## Arguments

Arguments passed to a meta-task are forwarded to every invocation. They are placed before fixed invocation arguments, so fixed arguments take precedence for DotNetDo task parameters.

```toml
[tasks]
release-test = [
  "build --configuration Release",
  "test-csharp --configuration Release"
]
```

`do release-test --configuration Debug` invokes both tasks with the inherited `Debug` value followed by the fixed `Release` value. Tasks using custom argument parsing must tolerate inherited arguments intended primarily for sibling tasks.

## Execution

- Invocations run sequentially.
- The first non-zero result stops the meta-task and becomes its result.
- Meta-tasks may invoke other meta-tasks.
- DotNetDo validates all configured references and cycles before executing anything.
- Empty meta-tasks and invocation strings are invalid.
- Nested meta-tasks are traversed in the current DotNetDo process. C# tasks retain normal separate-process execution.
- There is no cleanup phase, parallelism, condition, freshness detection, caching, or execution deduplication.

## Discovery and help

C# tasks and meta-tasks share one name namespace and the same name grammar. A name collision is invalid configuration.

Bare `do` lists both representations together alphabetically. `do :help <meta-task>` displays the authored invocation sequence and explains argument forwarding; it does not merge parameter declarations from child tasks.
