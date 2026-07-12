using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;

namespace DotNetDo;

public static partial class Do
{
    /// <summary>Exposes the configured value or operation to script authors.</summary>
    public static Solution Solution
    {
        get => field ??= Solution.DiscoverAsync().GetAwaiter().GetResult();
        set => field = value ?? throw new ArgumentNullException(nameof(value));
    }
}

/// <summary>An immutable snapshot of a solution file and its declared projects.</summary>
public sealed class Solution
{
    static readonly Guid SolutionFolderType = new("2150E333-8FDC-42A3-9474-1A3956D46DE8");
    readonly Dictionary<string, ProjectInfo> _projectsByPath;

    Solution(AbsolutePath path, SolutionModel model)
    {
        Path = path;
        Directory = path.Parent!;
        Projects = model.SolutionProjects
            .Where(project => project.TypeId != SolutionFolderType)
            .Select(CreateProject)
            .OrderBy(project => project.SolutionPath, StringComparer.Ordinal)
            .ToArray();
        _projectsByPath = Projects.ToDictionary(project => project.SolutionPath, StringComparer.Ordinal);
    }

    /// <summary>Loads and parses the referenced project or solution resource.</summary>
    public static Task<Solution> Load(string path, CancellationToken cancellationToken = default) =>
        Load(Resolve(path), cancellationToken);

    /// <summary>Loads and parses the referenced project or solution resource.</summary>
    public static async Task<Solution> Load(AbsolutePath path, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(path);
        Validate(path);

        var serializer = SolutionSerializers.GetSerializerByMoniker(path)
            ?? throw new NotSupportedException($"No solution serializer supports '{path}'.");
        var model = await serializer.OpenAsync(path, cancellationToken).ConfigureAwait(false);
        return new Solution(path, model);
    }

    /// <summary>The absolute filesystem path.</summary>
    public AbsolutePath Path { get; }
    /// <summary>The containing directory.</summary>
    public AbsolutePath Directory { get; }
    /// <summary>Projects declared by the loaded solution.</summary>
    public IReadOnlyList<ProjectInfo> Projects { get; }

    /// <summary>Exposes the configured value or operation to script authors.</summary>
    public ProjectInfo this[string solutionPath]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(solutionPath);
            if (_projectsByPath.TryGetValue(solutionPath, out var project))
                return project;

            throw new KeyNotFoundException(
                $"Project '{solutionPath}' was not found in '{Path}'. Available projects: {string.Join(", ", _projectsByPath.Keys.Order(StringComparer.Ordinal))}.");
        }
    }

    internal static Task<Solution> DiscoverAsync()
    {
        for (var directory = new DirectoryInfo(Do.RootDirectory); directory is not null; directory = directory.Parent)
        {
            var matches = directory.EnumerateFiles("*.sln", SearchOption.TopDirectoryOnly)
                .Concat(directory.EnumerateFiles("*.slnx", SearchOption.TopDirectoryOnly))
                .OrderBy(file => file.Name, StringComparer.Ordinal)
                .ToArray();
            switch (matches.Length)
            {
                case 0:
                    continue;
                case > 1:
                    throw new InvalidOperationException($"Multiple solutions were found in '{directory.FullName}': {string.Join(", ", matches.Select(file => file.Name))}.");
                default:
                    return Load(AbsolutePath.Parse(matches[0].FullName));
            }
        }

        throw new FileNotFoundException($"No .sln or .slnx file was found in '{Do.RootDirectory}' or its ancestors.");
    }

    ProjectInfo CreateProject(SolutionProjectModel project)
    {
        var parentPath = project.Parent?.Path.Trim('/');
        var solutionPath = string.IsNullOrEmpty(parentPath)
            ? project.ActualDisplayName
            : $"{parentPath}/{project.ActualDisplayName}";
        var projectPath = AbsolutePath.Parse(System.IO.Path.GetFullPath(project.FilePath, Directory));
        return new ProjectInfo(solutionPath.Replace('\\', '/'), projectPath);
    }

    static AbsolutePath Resolve(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        var fullPath = System.IO.Path.GetFullPath(path, Do.WorkingDirectory);
        return AbsolutePath.Parse(fullPath);
    }

    static void Validate(AbsolutePath path)
    {
        if (path.Extension is not ".sln" and not ".slnx")
            throw new ArgumentException("A solution path must end in .sln or .slnx.", nameof(path));
        if (!File.Exists(path))
            throw new FileNotFoundException($"Solution file '{path}' does not exist.", path);
    }
}
