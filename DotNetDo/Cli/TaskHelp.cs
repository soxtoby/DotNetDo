using System.Text.RegularExpressions;

namespace DotNetDo.Cli;

static partial class TaskHelp
{
    public static int Show(string taskName)
    {
        var catalog = TaskCatalog.Load();
        if (catalog.TryGetMetaTask(taskName, out var invocations))
        {
            Console.WriteLine($"Usage: dotnet do {taskName} [options...]");
            Console.WriteLine();
            Console.WriteLine("Invocations:");
            foreach (var invocation in invocations)
                Console.WriteLine($"  {invocation.TaskName}{(invocation.Arguments.Length == 0 ? "" : $" {invocation.Arguments}")}");
            Console.WriteLine();
            Console.WriteLine("Arguments are forwarded to each task.");
            return 0;
        }

        var relativeFile = catalog.ScriptsPath / $"{taskName}.cs";
        var file = Do.RootDirectory / relativeFile;
        if (!file.IsExistingFile)
        {
            Console.Error.WriteLine($"Task '{taskName}' does not exist.");
            return 1;
        }

        var parameters = Discover(file).ToArray();

        Console.WriteLine($"Usage: dotnet do {taskName} [options...]");

        if (parameters.Length == 0)
            return 0;

        Console.WriteLine();
        Console.WriteLine("Options:");

        foreach (var parameter in parameters)
            Console.WriteLine(Format(parameter));

        return 0;
    }

    internal static IEnumerable<TaskParameter> Discover(string fileName)
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
                var genericType = match.Groups["type"].Success ? match.Groups["type"].Value : null;
                var values = (secret, type, genericType) switch
                    {
                        (true, _, _) => [],
                        (false, "bool", _) => ["true", "false"],
                        (false, _, not null) => DiscoverEnumMembers(source, genericType),
                        _ => [],
                    };

                yield return new TaskParameter(name, type, description, defaultValue, required, secret, values);
            }
        }
    }

    static string[] DiscoverEnumMembers(string source, string type)
    {
        var simpleType = type.Split('.').Last().Trim().TrimEnd('?');
        var declaration = Regex.Match(
            source,
            $@"(?ms)^[ \t]*(?:(?:public|internal|private|protected)[ \t]+)*enum[ \t]+{Regex.Escape(simpleType)}\b[^{{]*\{{(?<body>.*?)^[ \t]*\}}");
        if (!declaration.Success)
            return [];

        return
        [
            .. EnumMemberRegex().Matches(declaration.Groups["body"].Value)
                .Select(match => match.Groups["name"].Value)
                .Distinct(StringComparer.Ordinal)
        ];
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

        var value = arguments[1].StartsWith("defaultValue:", StringComparison.Ordinal)
            ? arguments[1]["defaultValue:".Length..].Trim()
            : arguments[1];

        if (value == "null")
            return null;

        return TryReadString(value) ?? value;
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

    static string Format(TaskParameter parameter)
    {
        var line = $"  --{parameter.Name} <{parameter.Type}>";

        if (!string.IsNullOrWhiteSpace(parameter.Description))
            line += $"  {parameter.Description}";

        if (parameter.Required)
            line += "  required";

        if (parameter.Secret)
            line += "  secret";
        else if (parameter.DefaultValue is not null)
            line += $"  default: {parameter.DefaultValue}";

        return line;
    }

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

    [GeneratedRegex(@"(?m)^(?:[ \t]*\[[^\r\n]*\][ \t]*\r?\n)*[ \t]*(?:\[[^\r\n]*\][ \t]*)*(?<name>[A-Za-z_][A-Za-z0-9_]*)[ \t]*(?:=|,|$)")]
    private static partial Regex EnumMemberRegex();

    internal sealed record TaskParameter(
        string Name,
        string Type,
        string? Description,
        string? DefaultValue,
        bool Required,
        bool Secret,
        IReadOnlyList<string> Values)
    {
        public bool HasSameCompletionMetadata(TaskParameter other) =>
            Name == other.Name
            && Type == other.Type
            && Description == other.Description
            && Required == other.Required
            && Secret == other.Secret
            && Values.SequenceEqual(other.Values, StringComparer.Ordinal);
    }
}
