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

        if (scriptsDirectory.IsExistingDirectory)
        {
            foreach (var file in scriptsDirectory.GlobFiles("**/*.cs")
                .Select(path => solutionPath.Parent.RelativePathTo(path).UnixPath)
                .Order(StringComparer.Ordinal))
            {
                folder.AddFile(file);
            }
        }

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
        folder.Add(new XElement("File", new XAttribute("Path", RelativeGlob(solutionPath.Parent, scriptsDirectory))));
        solutionPath.WriteText(document.ToString());
        return Task.CompletedTask;
    }

    static bool IsCSharp(string path) => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);

    static string RelativeGlob(AbsolutePath solutionDirectory, AbsolutePath scriptsDirectory)
    {
        var path = solutionDirectory.RelativePathTo(scriptsDirectory).UnixPath;
        return path == "."
            ? "**/*.cs"
            : $"{path}/**/*.cs";
    }
}
