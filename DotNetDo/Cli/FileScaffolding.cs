namespace DotNetDo.Cli;

static class FileScaffolding
{
    public static void MakeExecutableIfUnix(string fileName)
    {
        if (OperatingSystem.IsWindows())
            return;

        try
        {
            File.SetUnixFileMode(fileName,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
        }
        catch
        {
            // Best effort for filesystems without Unix mode support.
        }
    }
}
