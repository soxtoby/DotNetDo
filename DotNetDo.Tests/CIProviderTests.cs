using Serilog;
using Xunit;

namespace DotNetDo.Tests;

public sealed class CIProviderTests
{
    static readonly Lock ConsoleGate = new();

    [Fact]
    public void GitHub_annotations_escape_data_and_properties()
    {
        var output = Capture(() => new GitHubActions().Warning("bad%\nvalue", new()
        {
            Title = "title,one",
            File = RelativePath.Parse("src/file.cs"),
            Line = 12
        }));

        Assert.Equal("::warning title=title%2Cone,file=src/file.cs,line=12::bad%25%0Avalue", output);
    }

    [Fact]
    public void Azure_issues_escape_command_values()
    {
        var output = Capture(() => new AzurePipelines().LogIssue("bad;%\nvalue", new()
        {
            Type = AzureIssueType.Warning,
            SourcePath = RelativePath.Parse("src/file.cs"),
            LineNumber = 12
        }));

        Assert.Equal("##vso[task.logissue type=warning;sourcepath=src/file.cs;linenumber=12;]bad%3B%AZP25%0Avalue", output);
    }

    [Fact]
    public void GitHub_multiline_values_use_environment_files()
    {
        var path = Path.GetTempFileName();
        var previous = Environment.GetEnvironmentVariable("GITHUB_OUTPUT");
        try
        {
            Environment.SetEnvironmentVariable("GITHUB_OUTPUT", path);
            new GitHubActions().SetOutput("result", "line one\nline two");
            var text = File.ReadAllText(path);
            Assert.StartsWith("result<<dotnetdo_", text);
            Assert.Contains($"{Environment.NewLine}line one\nline two{Environment.NewLine}dotnetdo_", text);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GITHUB_OUTPUT", previous);
            File.Delete(path);
        }
    }

    [Fact]
    public void GitHub_missing_command_file_is_explicit()
    {
        var previous = Environment.GetEnvironmentVariable("GITHUB_OUTPUT");
        try
        {
            Environment.SetEnvironmentVariable("GITHUB_OUTPUT", null);
            var exception = Assert.Throws<InvalidOperationException>(() => new GitHubActions().SetOutput("result", "value"));
            Assert.Contains("GITHUB_OUTPUT", exception.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GITHUB_OUTPUT", previous);
        }
    }

    [Fact]
    public void Metadata_is_typed_and_snapshotted()
    {
        var previousId = Environment.GetEnvironmentVariable("GITHUB_RUN_ID");
        var previousWorkspace = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE");
        try
        {
            Environment.SetEnvironmentVariable("GITHUB_RUN_ID", "42");
            Environment.SetEnvironmentVariable("GITHUB_WORKSPACE", Path.GetFullPath("workspace"));
            var github = new GitHubActions();

            Environment.SetEnvironmentVariable("GITHUB_RUN_ID", "43");
            Assert.Equal(42, github.Run.Id);
            Assert.Equal(AbsolutePath.Parse(Path.GetFullPath("workspace")), github.Runner.Workspace);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GITHUB_RUN_ID", previousId);
            Environment.SetEnvironmentVariable("GITHUB_WORKSPACE", previousWorkspace);
        }
    }

    [Fact]
    public void CI_sink_emits_debug_to_every_active_provider()
    {
        var output = Capture(() =>
        {
            using var logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Sink(new CISink(new GitHubActions(), new AzurePipelines()))
                .CreateLogger();
            logger.Debug("details");
        });

        Assert.Equal($"::debug::details{Environment.NewLine}##[debug]details", output);
    }

    [Fact]
    public void CI_sink_writes_information_once()
    {
        var output = Capture(() =>
        {
            using var logger = new LoggerConfiguration()
                .WriteTo.Sink(new CISink(new GitHubActions(), new AzurePipelines()))
                .CreateLogger();
            logger.Information("ordinary");
        });

        Assert.Equal("ordinary", output);
    }

    static string Capture(Action action)
    {
        lock (ConsoleGate)
        {
            var original = Console.Out;
            using var output = new StringWriter();
            try
            {
                Console.SetOut(output);
                action();
                return output.ToString().TrimEnd('\r', '\n');
            }
            finally
            {
                Console.SetOut(original);
            }
        }
    }
}
