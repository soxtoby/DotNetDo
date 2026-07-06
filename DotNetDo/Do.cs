namespace DotNetDo;

public static partial class Do
{
    public static string[] Args => Environment.GetCommandLineArgs().Skip(1).ToArray();
}
