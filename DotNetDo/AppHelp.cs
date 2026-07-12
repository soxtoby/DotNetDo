using System.Text.RegularExpressions;

namespace DotNetDo;

static partial class AppHelp
{
    public static int Show(string appName)
    {
        var relativeFile = Do.ScriptsPath / $"{appName}.cs";
        var file = Do.RootDirectory / relativeFile;
        if (!file.IsExistingFile)
        {
            Console.Error.WriteLine($"{relativeFile} does not exist.");
            return 1;
        }

        var parameters = Discover(file).ToArray();

        Console.WriteLine($"Usage: do {appName} [options...]");

        if (parameters.Length == 0)
            return 0;

        Console.WriteLine();
        Console.WriteLine("Options:");

        foreach (var parameter in parameters)
            Console.WriteLine(Format(parameter));

        return 0;
    }

    static IEnumerable<AppParameter> Discover(string fileName)
    {
        var source = File.ReadAllText(fileName);
        foreach (Match match in ParameterRegex().Matches(source))
        {
            var arguments = ParseArguments(match.Groups["arguments"].Value);
            if (arguments.Length != 0 && TryReadString(arguments[0]) is { } name)
            {
                var defaultValue = DefaultValue(arguments);
                var description = Description(arguments);
                var required = match.Groups["required"].Success;
                var secret = match.Groups["kind"].Value == "Secret";
                var type = secret
                    ? "string"
                    : match.Groups["type"].Success
                        ? FriendlyTypeName(match.Groups["type"].Value)
                        : InferType(defaultValue);

                yield return new AppParameter(name, type, description, defaultValue, required, secret);
            }
        }
    }

    static string[] ParseArguments(string text)
    {
        var arguments = new List<string>();
        var start = 0;
        var inString = false;
        var escaped = false;

        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            if (inString)
            {
                if (escaped)
                    escaped = false;
                else if (character == '\\')
                    escaped = true;
                else if (character == '"')
                    inString = false;
            }
            else if (character == '"')
            {
                inString = true;
            }
            else if (character == ',')
            {
                arguments.Add(text[start..index].Trim());
                start = index + 1;
            }
        }

        arguments.Add(text[start..].Trim());
        return [.. arguments.Where(argument => argument.Length > 0)];
    }

    static string? DefaultValue(string[] arguments)
    {
        if (arguments.Length < 2 || arguments[1].StartsWith("description:", StringComparison.Ordinal))
            return null;

        if (arguments[1] == "null")
            return null;

        return TryReadString(arguments[1]) ?? arguments[1];
    }

    static string? Description(string[] arguments)
    {
        foreach (var argument in arguments.Skip(1))
        {
            if (argument.StartsWith("description:", StringComparison.Ordinal))
                return TryReadString(argument["description:".Length..].Trim());
        }

        return arguments.Length >= 3 ? TryReadString(arguments[2]) : null;
    }

    static string? TryReadString(string value)
    {
        value = value.Trim();
        return value is ['"', _, .., '"']
            ? value[1..^1]
            : null;
    }

    static string Format(AppParameter parameter)
    {
        var line = $"  --{parameter.Name} <{parameter.Type}>";

        if (!string.IsNullOrWhiteSpace(parameter.Description))
            line += $"  {parameter.Description}";

        line += $"  env: {EnvironmentName(parameter.Name)}";

        if (parameter.Required)
            line += "  required";

        if (parameter.Secret)
            line += "  secret";
        else if (parameter.DefaultValue is not null)
            line += $"  default: {parameter.DefaultValue}";

        return line;
    }

    static string EnvironmentName(string name) =>
        "DOTNETDO_" + NonAlphaNumericRegex().Replace(name, "_").ToUpperInvariant();

    static string FriendlyTypeName(string type) =>
        type switch
            {
                "String" or "string" => "string",
                "Boolean" or "bool" => "bool",
                "Int32" or "int" => "int",
                _ => type
            };

    static string InferType(string? defaultValue) =>
        defaultValue switch
            {
                "true" or "false" => "bool",
                not null when int.TryParse(defaultValue, out _) => "int",
                _ => "string"
            };

    [GeneratedRegex(@"Do\.(?<kind>Param|Secret)(?:<(?<type>[^>]+)>)?\((?<arguments>[^)]*)\)(?<required>\.Required\(\))?")]
    private static partial Regex ParameterRegex();

    [GeneratedRegex("[^A-Za-z0-9]+")]
    private static partial Regex NonAlphaNumericRegex();

    sealed record AppParameter(
        string Name,
        string Type,
        string? Description,
        string? DefaultValue,
        bool Required,
        bool Secret);
}
