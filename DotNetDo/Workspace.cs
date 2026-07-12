namespace DotNetDo;

public static partial class Do
{
    public static AbsolutePath WorkingDirectory
    {
        get => AbsolutePath.Parse(Environment.CurrentDirectory);
        set => Directory.SetCurrentDirectory(value);
    }

    public static AbsolutePath RootDirectory
    {
        get
        {
            if (field is not null)
                return field;

            var workingDirectory = WorkingDirectory;
            for (var directory = workingDirectory; directory is not null; directory = directory.Parent)
                if ((directory / "dotnetdo.toml").IsExistingFile)
                    return field = directory;

            return workingDirectory;
        }
    }
}
