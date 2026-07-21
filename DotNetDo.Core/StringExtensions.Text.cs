namespace DotNetDo;

public static partial class StringExtensions
{
    /// <summary>Splits text into lines using standard text-reader newline semantics.</summary>
    public static string[] SplitLines(this string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        using var reader = new StringReader(value);
        var lines = new List<string>();

        while (reader.ReadLine() is { } line)
            lines.Add(line);

        return [..lines];
    }

    extension(string? value)
    {
        /// <summary>Returns whether the value is null or empty.</summary>
        public bool IsNullOrEmpty() => string.IsNullOrEmpty(value);

        /// <summary>Returns whether the value is null, empty, or consists only of whitespace.</summary>
        public bool IsNullOrWhiteSpace() => string.IsNullOrWhiteSpace(value);
    }
}