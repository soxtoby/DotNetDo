using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace DotNetDo;

public static partial class StringExtensions
{
    extension(string input)
    {
        /// <summary>Returns whether the regular-expression pattern matches the input.</summary>
        public bool IsRegexMatch(
            [StringSyntax(StringSyntaxAttribute.Regex)]
            string pattern,
            RegexOptions options = RegexOptions.None,
            TimeSpan? timeout = null
        ) =>
            timeout is { } value
                ? Regex.IsMatch(input, pattern, options, value)
                : Regex.IsMatch(input, pattern, options);

        /// <summary>Returns the first regular-expression match in the input.</summary>
        public Match RegexMatch(
            [StringSyntax(StringSyntaxAttribute.Regex)]
            string pattern,
            RegexOptions options = RegexOptions.None,
            TimeSpan? timeout = null
        ) =>
            timeout is { } value
                ? Regex.Match(input, pattern, options, value)
                : Regex.Match(input, pattern, options);

        /// <summary>Returns every regular-expression match in the input.</summary>
        public MatchCollection RegexMatches(
            [StringSyntax(StringSyntaxAttribute.Regex)]
            string pattern,
            RegexOptions options = RegexOptions.None,
            TimeSpan? timeout = null
        ) =>
            timeout is { } value
                ? Regex.Matches(input, pattern, options, value)
                : Regex.Matches(input, pattern, options);

        /// <summary>Replaces regular-expression matches using a replacement pattern.</summary>
        public string RegexReplace(
            [StringSyntax(StringSyntaxAttribute.Regex)]
            string pattern,
            string replacement,
            RegexOptions options = RegexOptions.None,
            TimeSpan? timeout = null
        ) =>
            timeout is { } value
                ? Regex.Replace(input, pattern, replacement, options, value)
                : Regex.Replace(input, pattern, replacement, options);

        /// <summary>Replaces regular-expression matches using a match evaluator.</summary>
        public string RegexReplace(
            [StringSyntax(StringSyntaxAttribute.Regex)]
            string pattern,
            MatchEvaluator evaluator,
            RegexOptions options = RegexOptions.None,
            TimeSpan? timeout = null
        ) =>
            timeout is { } value
                ? Regex.Replace(input, pattern, evaluator, options, value)
                : Regex.Replace(input, pattern, evaluator, options);

        /// <summary>Splits the input at regular-expression matches.</summary>
        public string[] RegexSplit(
            [StringSyntax(StringSyntaxAttribute.Regex)]
            string pattern,
            RegexOptions options = RegexOptions.None,
            TimeSpan? timeout = null
        ) =>
            timeout is { } value
                ? Regex.Split(input, pattern, options, value)
                : Regex.Split(input, pattern, options);
    }
}