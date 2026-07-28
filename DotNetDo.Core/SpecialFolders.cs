namespace DotNetDo;

public static partial class Do
{
    /// <summary>Gets the current user's profile directory without checking its existence.</summary>
    /// <exception cref="DirectoryNotFoundException">The platform does not provide the directory.</exception>
    public static AbsolutePath UserProfile => SpecialFolders.Get(Environment.SpecialFolder.UserProfile);

    /// <summary>Gets the current user's documents directory without checking its existence.</summary>
    /// <exception cref="DirectoryNotFoundException">The platform does not provide the directory.</exception>
    public static AbsolutePath Documents => SpecialFolders.Get(Environment.SpecialFolder.MyDocuments);

    /// <summary>Gets the current user's roaming application-data directory without checking its existence.</summary>
    /// <exception cref="DirectoryNotFoundException">The platform does not provide the directory.</exception>
    public static AbsolutePath ApplicationData => SpecialFolders.Get(Environment.SpecialFolder.ApplicationData);

    /// <summary>Gets the current user's local application-data directory without checking its existence.</summary>
    /// <exception cref="DirectoryNotFoundException">The platform does not provide the directory.</exception>
    public static AbsolutePath LocalApplicationData => SpecialFolders.Get(Environment.SpecialFolder.LocalApplicationData);

    /// <summary>Gets the platform's program-files directory without checking its existence.</summary>
    /// <exception cref="DirectoryNotFoundException">The platform does not provide the directory.</exception>
    public static AbsolutePath ProgramFiles => SpecialFolders.Get(Environment.SpecialFolder.ProgramFiles);

    /// <summary>Gets the platform's 32-bit program-files directory without checking its existence.</summary>
    /// <exception cref="DirectoryNotFoundException">The platform does not provide the directory.</exception>
    public static AbsolutePath ProgramFilesX86 => SpecialFolders.Get(Environment.SpecialFolder.ProgramFilesX86);
}

static class SpecialFolders
{
    public static AbsolutePath Get(Environment.SpecialFolder folder) =>
        Parse(Environment.GetFolderPath(folder, Environment.SpecialFolderOption.DoNotVerify), folder);

    internal static AbsolutePath Parse(string path, Environment.SpecialFolder folder) =>
        string.IsNullOrEmpty(path)
            ? throw new DirectoryNotFoundException($"{folder} directory is unavailable.")
            : AbsolutePath.Parse(path);
}
