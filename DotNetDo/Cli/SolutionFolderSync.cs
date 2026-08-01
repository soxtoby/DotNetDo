using Microsoft.VisualStudio.SolutionPersistence.Serializer;
using System.Xml.Linq;

namespace DotNetDo.Cli;

static class SolutionFolderSync
{
    public static Task Run(AbsolutePath solutionPath, AbsolutePath scriptsDirectory, string folderName) =>
        solutionPath.Extension switch
            {
                ".sln" => SyncSln(solutionPath, scriptsDirectory, folderName),
                ".slnx" => SyncSlnx(solutionPath, scriptsDirectory, folderName),
                _ => throw new NotSupportedException($"No solution serializer supports '{solutionPath}'.")
            };

    static async Task SyncSln(AbsolutePath solutionPath, AbsolutePath scriptsDirectory, string folderName)
    {
        var serializer = SolutionSerializers.GetSerializerByMoniker(solutionPath)
            ?? throw new NotSupportedException($"No solution serializer supports '{solutionPath}'.");
        var model = await serializer.OpenAsync(solutionPath, CancellationToken.None);
        var folder = model.SolutionFolders.SingleOrDefault(candidate => candidate.Parent is null && candidate.Name == folderName)
            ?? model.AddFolder(folderName);

        foreach (var file in (folder.Files ?? []).Where(IsCSharp).ToArray())
            folder.RemoveFile(file);

        foreach (var file in Files(solutionPath.Parent, scriptsDirectory))
            folder.AddFile(file.WindowsPath);

        await serializer.SaveAsync(solutionPath, model, CancellationToken.None);
    }

    static Task SyncSlnx(AbsolutePath solutionPath, AbsolutePath scriptsDirectory, string folderName)
    {
        var document = XDocument.Parse(solutionPath.ReadText());
        var root = document.Root ?? throw new InvalidDataException($"Solution '{solutionPath}' has no root element.");
        var folderPath = $"/{folderName}/";
        var folder = root.Elements("Folder")
                .SingleOrDefault(element => (string?)element.Attribute("Name") == folderPath)
            ?? new XElement("Folder", new XAttribute("Name", folderPath));
        if (folder.Parent is null)
            root.Add(folder);

        folder.Elements("File")
            .Where(element => IsCSharp((string?)element.Attribute("Path") ?? ""))
            .Remove();
        foreach (var file in Files(solutionPath.Parent, scriptsDirectory))
            folder.Add(new XElement("File", new XAttribute("Path", file.UnixPath)));

        solutionPath.WriteText(document.ToString());
        return Task.CompletedTask;
    }

    static bool IsCSharp(string path) => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);

    static IEnumerable<RelativePath> Files(AbsolutePath solutionDirectory, AbsolutePath scriptsDirectory) =>
        scriptsDirectory.IsExistingDirectory
            ? scriptsDirectory.GlobFiles("**/*.cs")
                .Select(solutionDirectory.RelativePathTo)
                .OrderBy(path => path.UnixPath, StringComparer.Ordinal)
            : [];
}
