namespace DotNetDo.Cli;

static class CompletionCommand
{
    const string StartMarker = "# >>> DotNetDo completion >>>";
    const string EndMarker = "# <<< DotNetDo completion <<<";

    public static int Run(string[] args)
    {
        if (!TryParse(args, out var uninstall, out var shell, out var error))
            return Fail(error);

        try
        {
            return Run(
                uninstall,
                shell!,
                Do.UserProfile,
                Do.LocalApplicationData / "DotNetDo" / "completion",
                Console.Out,
                Do.Documents);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Fail(exception.Message);
        }
    }

    internal static int Run(
        bool uninstall,
        string shell,
        AbsolutePath userProfile,
        AbsolutePath dataDirectory,
        TextWriter? output = null,
        AbsolutePath? documentsDirectory = null)
    {
        var profile = ProfilePath(shell, userProfile, documentsDirectory);
        var adapter = dataDirectory / $"dotnetdo-completion.{Extension(shell)}";

        if (uninstall)
        {
            RemoveProfileBlock(profile);
            if (adapter.IsExistingFile)
                adapter.Delete();
            output?.WriteLine($"Removed DotNetDo completion for {shell}.");
            return 0;
        }

        dataDirectory.EnsureDirectoryExists();
        WriteAtomic(adapter, Adapter(shell));
        InstallProfileBlock(profile, SourceLine(shell, adapter));
        output?.WriteLine($"Installed DotNetDo completion for {shell}.");
        output?.WriteLine($"Restart {shell} to activate it.");
        return 0;
    }

    static bool TryParse(string[] args, out bool uninstall, out string? shell, out string error)
    {
        uninstall = false;
        shell = null;
        error = "";

        var values = args.Skip(1).ToArray();
        if (values.FirstOrDefault() == "uninstall")
        {
            uninstall = true;
            values = values[1..];
        }

        if (values.Length > 1)
        {
            error = "Usage: dotnet do :completion [pwsh|bash|zsh] | :completion uninstall [pwsh|bash|zsh]";
            return false;
        }

        shell = values.SingleOrDefault() ?? DetectShell();
        if (shell is not ("pwsh" or "bash" or "zsh"))
        {
            error = "Could not detect a supported shell. Specify pwsh, bash, or zsh.";
            return false;
        }

        return true;
    }

    static string? DetectShell()
    {
        if (OperatingSystem.IsWindows())
            return "pwsh";

        var shell = Environment.GetEnvironmentVariable("SHELL");
        var executable = AbsolutePath.TryParse(shell, out var path) ? path.Name : shell;
        return executable is "bash" or "zsh" ? executable : null;
    }

    static AbsolutePath ProfilePath(string shell, AbsolutePath userProfile, AbsolutePath? documentsDirectory) =>
        shell switch
        {
            "pwsh" => (documentsDirectory ?? userProfile / "Documents") / "PowerShell/Microsoft.PowerShell_profile.ps1",
            "bash" => userProfile / ".bashrc",
            "zsh" => userProfile / ".zshrc",
            _ => throw new ArgumentOutOfRangeException(nameof(shell)),
        };

    static string Extension(string shell) => shell == "pwsh" ? "ps1" : shell;

    static string SourceLine(string shell, AbsolutePath adapter)
    {
        var quoted = adapter.ToString().Replace("'", shell == "pwsh" ? "''" : "'\\''");
        return $". '{quoted}'";
    }

    static void InstallProfileBlock(AbsolutePath profile, string sourceLine)
    {
        var content = profile.IsExistingFile ? profile.ReadText() : "";
        content = WithoutProfileBlock(content).TrimEnd();
        if (content.Length != 0)
            content += Environment.NewLine + Environment.NewLine;
        content += $"{StartMarker}{Environment.NewLine}{sourceLine}{Environment.NewLine}{EndMarker}{Environment.NewLine}";
        WriteAtomic(profile, content);
    }

    static void RemoveProfileBlock(AbsolutePath profile)
    {
        if (!profile.IsExistingFile)
            return;

        var content = WithoutProfileBlock(profile.ReadText());
        WriteAtomic(profile, content);
    }

    static string WithoutProfileBlock(string content)
    {
        var start = content.IndexOf(StartMarker, StringComparison.Ordinal);
        var end = content.IndexOf(EndMarker, StringComparison.Ordinal);
        if (start < 0 && end < 0)
            return content;
        if (start < 0 || end < start
            || content.IndexOf(StartMarker, start + StartMarker.Length, StringComparison.Ordinal) >= 0
            || content.IndexOf(EndMarker, end + EndMarker.Length, StringComparison.Ordinal) >= 0)
            throw new InvalidDataException("The shell profile contains unmatched or duplicate DotNetDo completion markers.");

        end += EndMarker.Length;
        if (end < content.Length && content[end] == '\r')
            end++;
        if (end < content.Length && content[end] == '\n')
            end++;
        return content.Remove(start, end - start);
    }

    static void WriteAtomic(AbsolutePath path, string content)
    {
        var directory = path.Parent.EnsureDirectoryExists();
        var temporary = directory / $".{path.Name}.{Guid.NewGuid():N}.tmp";
        try
        {
            temporary.WriteText(content);
            temporary.MoveTo(path, new TransferOptions { Overwrite = true });
        }
        finally
        {
            if (temporary.IsExistingFile)
                temporary.Delete();
        }
    }

    static string Adapter(string shell) =>
        shell switch
        {
            "pwsh" => PowerShellAdapter,
            "bash" => BashAdapter,
            "zsh" => ZshAdapter,
            _ => throw new ArgumentOutOfRangeException(nameof(shell)),
        };

    const string PowerShellAdapter =
        """
        Register-ArgumentCompleter -Native -CommandName dotnet-do -ScriptBlock {
            param($wordToComplete, $commandAst, $cursorPosition)

            $tokens = @($commandAst.CommandElements | ForEach-Object {
                if ($_ -is [System.Management.Automation.Language.StringConstantExpressionAst]) { $_.Value }
                else { $_.Extent.Text }
            })
            if ($wordToComplete.Length -eq 0) {
                $tokens += ''
            }
            $active = $tokens.Count - 1

            & dotnet-do :complete $active -- @tokens 2>$null | ForEach-Object {
                $parts = $_ -split "`t", 2
                [System.Management.Automation.CompletionResult]::new($parts[0], $parts[0], 'ParameterValue', $(if ($parts.Count -gt 1) { $parts[1] } else { $parts[0] }))
            }
        }
        """;

    const string BashAdapter =
        """
        _dotnetdo_complete()
        {
            local item
            COMPREPLY=()
            while IFS= read -r item; do
                COMPREPLY+=("${item%%$'\t'*}")
            done < <(dotnet-do :complete "$COMP_CWORD" -- "${COMP_WORDS[@]}" 2>/dev/null)
        }
        complete -F _dotnetdo_complete dotnet-do
        """;

    const string ZshAdapter =
        """
        _dotnetdo_complete()
        {
            local line candidate detail
            local -a items
            while IFS= read -r line; do
                candidate="${line%%$'\t'*}"
                detail="${line#*$'\t'}"
                candidate="${candidate//:/\\:}"
                items+=("${candidate}:${detail}")
            done < <(dotnet-do :complete "$((CURRENT - 1))" -- "${words[@]}" 2>/dev/null)
            _describe 'DotNetDo' items
        }
        compdef _dotnetdo_complete dotnet-do
        """;

    static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 1;
    }
}
