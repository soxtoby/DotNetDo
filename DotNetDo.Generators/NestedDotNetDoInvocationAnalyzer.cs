using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace DotNetDo.Generators;

/// <summary>Warns when a task launches another DotNetDo task through <c>Do.Exec</c>.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NestedDotNetDoInvocationAnalyzer : DiagnosticAnalyzer
{
    /// <summary>The diagnostic identifier for nested DotNetDo execution.</summary>
    public const string DiagnosticId = "DND001";

    static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Do not invoke DotNetDo through Do.Exec",
        "Compose tasks with a configured meta-task or shared C# method instead of invoking DotNetDo through Do.Exec",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Recursively launching DotNetDo bypasses its task-composition model.");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;
        if (invocation.TargetMethod is
                {
                    Name: "Exec",
                    ContainingType.Name: "Do",
                    ContainingType.ContainingNamespace: { } containingNamespace,
                }
            && containingNamespace.ToDisplayString() == "DotNetDo")
        {
            var commandArgument = invocation.Arguments.FirstOrDefault(argument => 
                argument.Parameter is { Ordinal: 0, Type.SpecialType: SpecialType.System_String });
            var command = commandArgument?.Value.ConstantValue;

            if (command is { HasValue: true, Value: string value } && InvokesDotNetDo(value))
                context.ReportDiagnostic(Diagnostic.Create(Rule, commandArgument!.Syntax.GetLocation()));
        }
    }

    static bool InvokesDotNetDo(string command)
    {
        var trimmed = command.TrimStart();
        return StartsWithCommand(trimmed, "dotnet do")
            || StartsWithCommand(trimmed, "dotnet.exe do")
            || StartsWithCommand(trimmed, "dotnet-do")
            || StartsWithCommand(trimmed, "dotnet-do.exe")
            || StartsWithCommand(trimmed, "do")
            || StartsWithCommand(trimmed, "do.cmd")
            || StartsWithCommand(trimmed, "./do");
    }

    static bool StartsWithCommand(string command, string prefix) =>
        command.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
        && (command.Length == prefix.Length || char.IsWhiteSpace(command[prefix.Length]));
}
