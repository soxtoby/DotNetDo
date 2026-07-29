using System.Reflection;

namespace DotNetDo.Cli;

static class TaskScaffolding
{
    public static void Create(AbsolutePath file, string name)
    {
        var created = false;
        try
        {
            using (var stream = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream))
            {
                created = true;
                writer.Write(Template(name));
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

    static string Template(string name) =>
        $$"""
        #!/usr/bin/env dotnet
        #:package DotNetDo.Core@{{PackageVersion}}
        using DotNetDo;
        using Serilog;

        [assembly: TaskDescription("Says hello")]

        Log.Information("Hello from {Task}", "{{name}}");
        """;

    static string PackageVersion =>
        typeof(TaskScaffolding).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
            .InformationalVersion
            .Split('+', 2)[0];
}
