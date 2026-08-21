using System.Collections.Immutable;
using DotNetDo.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace DotNetDo.Tests;

public class NestedDotNetDoInvocationAnalyzerTests
{
    [Theory]
    [InlineData("Do.Exec(\"dotnet do build\");")]
    [InlineData("Do.Exec(\"dotnet-do build\");")]
    [InlineData("Do.Exec(\"  DOTNET.EXE do build\");")]
    [InlineData("const string command = \"dotnet do build\"; Do.Exec(command);")]
    [InlineData("const string task = \"build\"; Do.Exec($\"dotnet do {task}\");")]
    [InlineData("Do.Exec(options: null, command: \"dotnet do build\");")]
    public async Task Warns_for_compile_time_known_nested_invocations(string statement)
    {
        var diagnostics = await Analyze(statement);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(NestedDotNetDoInvocationAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Theory]
    [InlineData("Do.Exec(\"dotnet test\");")]
    [InlineData("Do.Exec(\"echo dotnet do build\");")]
    [InlineData("var command = \"dotnet do build\"; Do.Exec(command);")]
    [InlineData("Other.Exec(\"dotnet do build\");")]
    public async Task Ignores_other_or_dynamic_invocations(string statement) =>
        Assert.Empty(await Analyze(statement));

    static async Task<ImmutableArray<Diagnostic>> Analyze(string statement)
    {
        var source = $$"""
            using DotNetDo;

            static class Other
            {
                public static void Exec(string command) { }
            }

            static class Script
            {
                public static void Run()
                {
                    {{statement}}
                }
            }
            """;
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .Append(MetadataReference.CreateFromFile(typeof(Do).Assembly.Location));
        var compilation = CSharpCompilation.Create(
            "AnalyzerTests",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        Assert.DoesNotContain(compilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        return await compilation
            .WithAnalyzers([new NestedDotNetDoInvocationAnalyzer()])
            .GetAnalyzerDiagnosticsAsync();
    }
}
