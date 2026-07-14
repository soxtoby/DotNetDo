using System.Globalization;

namespace DotNetDo;

static class CIEnvironment
{
    public static bool IsTrue(string name) => Bool(name) == true;

    public static bool? Bool(string name) => ParseValue(name,
        value => value switch
            {
                "1" => true,
                "0" => false,
                _ => bool.Parse(value)
            });

    public static long? Long(string name) => ParseValue(name, value => long.Parse(value, CultureInfo.InvariantCulture));
    public static Guid? Guid(string name) => ParseValue(name, System.Guid.Parse);
    public static Uri? Uri(string name) => ParseReference(name, value => new Uri(value, UriKind.Absolute));
    public static DateTimeOffset? DateTime(string name) => ParseValue(name, value => DateTimeOffset.Parse(value, CultureInfo.InvariantCulture));
    public static AbsolutePath? Path(string name) => ParseReference(name, AbsolutePath.Parse);

    static T? ParseReference<T>(string name, Func<string, T> parse) where T : class
    {
        var value = String(name);
        return value is null ? null : parse(value);
    }

    static T? ParseValue<T>(string name, Func<string, T> parse) where T : struct
    {
        var value = String(name);
        return value is null ? null : parse(value);
    }

    public static string? String(string name) => Environment.GetEnvironmentVariable(name);
}