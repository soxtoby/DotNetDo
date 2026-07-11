using System.Text;
using Xunit;

namespace DotNetDo.Tests;

public sealed class SolutionTests
{
    [Theory]
    [InlineData("sln")]
    [InlineData("slnx")]
    public async Task Loads_projects_by_solution_path(string format)
    {
        using var workspace = Workspace.Create(format);

        var solution = await Solution.Load(workspace.SolutionPath, TestContext.Current.CancellationToken);
        var project = solution["src/App"];

        Assert.Equal("src/App", project.SolutionPath);
        Assert.Equal(AbsolutePath.Parse(workspace.ProjectPath), project.Path);
        Assert.Equal(project.Path.Parent, project.Directory);
        Assert.Throws<KeyNotFoundException>(() => solution["app"]);
    }

    [Fact]
    public async Task Loads_sdk_style_projects_with_global_properties()
    {
        using var workspace = Workspace.Create("sln");
        var project = (await Solution.Load(workspace.SolutionPath, TestContext.Current.CancellationToken))["src/App"];

        using var loaded = project.Load(new Dictionary<string, string>
        {
            ["Configuration"] = "Release"
        });

        Assert.Equal("net10.0", loaded.Project.GetPropertyValue("TargetFramework"));
        Assert.Equal("Release", loaded.Project.GetPropertyValue("Configuration"));
    }

    [Fact]
    public async Task Loads_old_style_dotnet_framework_projects()
    {
        using var workspace = Workspace.Create("sln", oldStyle: true);
        var project = (await Solution.Load(workspace.SolutionPath, TestContext.Current.CancellationToken))["src/App"];

        using var loaded = project.Load();

        Assert.Equal("v4.8", loaded.Project.GetPropertyValue("TargetFrameworkVersion"));
        Assert.Equal("Library", loaded.Project.GetPropertyValue("OutputType"));
    }

    sealed class Workspace : IDisposable
    {
        Workspace(string directory, string solutionPath, string projectPath)
        {
            Directory = directory;
            SolutionPath = solutionPath;
            ProjectPath = projectPath;
        }

        public string Directory { get; }
        public string SolutionPath { get; }
        public string ProjectPath { get; }

        public static Workspace Create(string format, bool oldStyle = false)
        {
            var directory = Path.Combine(Path.GetTempPath(), $"dotnetdo-{Guid.NewGuid():N}");
            var projectDirectory = Path.Combine(directory, "src", "App");
            System.IO.Directory.CreateDirectory(projectDirectory);
            var projectPath = Path.Combine(projectDirectory, "App.csproj");
            File.WriteAllText(projectPath, oldStyle ? OldProject : SdkProject, Encoding.UTF8);

            var solutionPath = Path.Combine(directory, $"Product.{format}");
            File.WriteAllText(solutionPath, format == "sln" ? Sln : Slnx, Encoding.UTF8);
            return new Workspace(directory, solutionPath, projectPath);
        }

        public void Dispose() => System.IO.Directory.Delete(Directory, recursive: true);

        const string SdkProject = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """;

        const string OldProject = """
            <Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
              <Import Project="$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props" Condition="Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')" />
              <PropertyGroup>
                <Configuration Condition=" '$(Configuration)' == '' ">Debug</Configuration>
                <Platform Condition=" '$(Platform)' == '' ">AnyCPU</Platform>
                <ProjectGuid>{4DA078A2-5838-4CA1-A30D-826BA491D564}</ProjectGuid>
                <OutputType>Library</OutputType>
                <RootNamespace>App</RootNamespace>
                <AssemblyName>App</AssemblyName>
                <TargetFrameworkVersion>v4.8</TargetFrameworkVersion>
                <_EnableDefaultWindowsPlatform>false</_EnableDefaultWindowsPlatform>
              </PropertyGroup>
              <Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />
            </Project>
            """;

        const string Sln = """
            Microsoft Visual Studio Solution File, Format Version 12.00
            # Visual Studio Version 17
            VisualStudioVersion = 17.0.31903.59
            MinimumVisualStudioVersion = 10.0.40219.1
            Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "src", "src", "{71D8C17B-F42F-44DD-B370-96C31C70D64F}"
            EndProject
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "App", "src\App\App.csproj", "{4DA078A2-5838-4CA1-A30D-826BA491D564}"
            EndProject
            Global
                GlobalSection(NestedProjects) = preSolution
                    {4DA078A2-5838-4CA1-A30D-826BA491D564} = {71D8C17B-F42F-44DD-B370-96C31C70D64F}
                EndGlobalSection
            EndGlobal
            """;

        const string Slnx = """
            <Solution>
              <Folder Name="/src/">
                <Project Path="src/App/App.csproj" />
              </Folder>
            </Solution>
            """;
    }
}
