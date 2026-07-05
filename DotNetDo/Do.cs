namespace DotNetDo;

public static class Do
{
    public static string[] Args => Environment.GetCommandLineArgs().Skip(1).ToArray();
}
