using NuGet.Versioning;

namespace DotNetDo.Cli;

static class TaskScaffolding
{
    public const string Package = "DotNetDo.Core";

    public static void Create(AbsolutePath file, string name, NuGetVersion packageVersion)
    {
        var created = false;
        try
        {
            using (var stream = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream))
            {
                created = true;
                writer.Write(Template(name, packageVersion));
            }
            FileScaffolding.MakeExecutableIfUnix(file);
        }
        catch
        {
            if (created)
                file.Delete();
            throw;
        }
    }

    static string Template(string name, NuGetVersion packageVersion) =>
        $$"""
        #!/usr/bin/env dotnet
        #:package DotNetDo.Core@{{packageVersion.ToNormalizedString()}}
        using DotNetDo;
        using Serilog;

        [assembly: TaskDescription("Says hello")]

        Log.Information("Hello from {Task}", "{{name}}");
        """;
}
