namespace DotNetDo;

static class ParameterPrompt
{
    public static bool TryRead<T>(string name, string? description, bool secret, out T value)
        where T : notnull
    {
        if (!Do.IsLocalBuild || Console.IsInputRedirected || Console.IsOutputRedirected)
        {
            value = default!;
            return false;
        }

        if (!description.IsNullOrWhiteSpace())
            Console.WriteLine(description);

        while (true)
        {
            Console.Write($"Required parameter '{name}' ({FriendlyTypeName(typeof(T))}): ");
            var input = secret ? ReadSecret() : Console.ReadLine();
            if (input is null)
            {
                value = default!;
                return false;
            }

            if (TryConvert(input, out value, out var error))
                return true;

            Console.WriteLine(error);
        }
    }

    public static bool TryRead(
        string name,
        string? type,
        string? description,
        bool secret,
        IReadOnlyList<string> values,
        out string value)
    {
        if (!Do.IsLocalBuild || Console.IsInputRedirected || Console.IsOutputRedirected)
        {
            value = "";
            return false;
        }

        if (!description.IsNullOrWhiteSpace())
            Console.WriteLine(description);
        while (true)
        {
            Console.Write($"Required parameter '{name}'{(type is null ? "" : $" ({type})")}: ");
            var input = secret ? ReadSecret() : Console.ReadLine();
            if (input is null)
            {
                value = "";
                return false;
            }
            if (TryNormalize(input, type, values, out value))
                return true;
            Console.WriteLine($"Value could not be parsed as {type}.");
        }
    }

    internal static bool TryConvert<T>(string input, out T value, out string? error)
        where T : notnull
    {
        input = NormalizeBoolean<T>(input);
        try
        {
            value = TaskParameterConfiguration.Create(["--value", input]).Read<T>("value").Value;
            error = null;
            return value is not null;
        }
        catch
        {
            value = default!;
            error = $"Value could not be parsed as {FriendlyTypeName(typeof(T))}.";
            return false;
        }
    }

    static string NormalizeBoolean<T>(string input) =>
        typeof(T) == typeof(bool)
            ? input.ToLowerInvariant() switch
                {
                    "y" or "yes" => "true",
                    "n" or "no" => "false",
                    _ => input
                }
            : input;

    internal static bool TryNormalize(string input, string? type, IReadOnlyList<string> values, out string value)
    {
        value = input;
        switch (type)
        {
            case null or "string":
                return true;
            case "bool":
                value = input.ToLowerInvariant() switch
                    {
                        "y" or "yes" => "true",
                        "n" or "no" => "false",
                        _ => input
                    };
                return TryConvertDiscovered<bool>(value, out value);
            case "int":
                return TryConvertDiscovered<int>(input, out value);
            default:
                return values.Count == 0 || values.Contains(input, StringComparer.OrdinalIgnoreCase);
        }
    }

    static bool TryConvertDiscovered<T>(string input, out string value)
        where T : notnull
    {
        try
        {
            _ = TaskParameterConfiguration.Create(["--value", input]).Read<T>("value").Value;
            value = input;
            return true;
        }
        catch
        {
            value = input;
            return false;
        }
    }

    static string ReadSecret()
    {
        var value = new List<char>();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            switch (key.Key)
            {
                case ConsoleKey.Enter:
                    Console.WriteLine();
                    return new string([.. value]);
                case ConsoleKey.Backspace when value.Count != 0:
                    value.RemoveAt(value.Count - 1);
                    break;
                default:
                    if (!char.IsControl(key.KeyChar))
                        value.Add(key.KeyChar);
                    break;
            }
        }
    }

    static string FriendlyTypeName(Type type) =>
        type == typeof(string) ? "string"
        : type == typeof(bool) ? "bool"
        : type == typeof(int) ? "int"
        : type.Name;
}
