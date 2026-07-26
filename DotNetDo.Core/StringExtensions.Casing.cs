using System.Globalization;
using System.Text;

namespace DotNetDo;

public static partial class StringExtensions
{
    extension(string value)
    {
        /// <summary>Converts the value to camel case, treating non-letter and non-digit characters as word separators.</summary>
        public string ToCamelCase() => ConvertCase(value, WordCase.Camel, "");

        /// <summary>Converts the value to Pascal case, treating non-letter and non-digit characters as word separators.</summary>
        public string ToPascalCase() => ConvertCase(value, WordCase.Pascal, "");

        /// <summary>Converts the value to lower snake case, treating non-letter and non-digit characters as word separators.</summary>
        public string ToSnakeCaseLower() => ConvertCase(value, WordCase.Lower, "_");

        /// <summary>Converts the value to upper snake case, treating non-letter and non-digit characters as word separators.</summary>
        public string ToSnakeCaseUpper() => ConvertCase(value, WordCase.Upper, "_");

        /// <summary>Converts the value to lower kebab case, treating non-letter and non-digit characters as word separators.</summary>
        public string ToKebabCaseLower() => ConvertCase(value, WordCase.Lower, "-");

        /// <summary>Converts the value to upper kebab case, treating non-letter and non-digit characters as word separators.</summary>
        public string ToKebabCaseUpper() => ConvertCase(value, WordCase.Upper, "-");
    }

    static string ConvertCase(string value, WordCase wordCase, string separator)
    {
        ArgumentNullException.ThrowIfNull(value);

        var words = SplitWords(value.Normalize(NormalizationForm.FormC));
        var result = new StringBuilder(value.Length);

        for (var index = 0; index < words.Count; index++)
        {
            if (index > 0)
                result.Append(separator);

            AppendWord(result, words[index], wordCase, index);
        }

        return result.ToString();
    }

    static List<List<Rune>> SplitWords(string value)
    {
        var words = new List<List<Rune>>();
        var word = new List<Rune>();
        var runes = value.EnumerateRunes().ToArray();
        Rune? previous = null;

        for (var index = 0; index < runes.Length; index++)
        {
            var rune = runes[index];
            if (!Rune.IsLetter(rune) && Rune.GetUnicodeCategory(rune) != UnicodeCategory.DecimalDigitNumber)
            {
                AddWord(words, word);
                previous = null;
                continue;
            }

            var next = index + 1 < runes.Length ? runes[index + 1] : (Rune?)null;
            if (previous is { } prior && StartsWord(prior, rune, next))
                AddWord(words, word);

            word.Add(rune);
            previous = rune;
        }

        AddWord(words, word);
        return words;
    }

    static bool StartsWord(Rune previous, Rune current, Rune? next) =>
        Rune.IsUpper(current)
        && (Rune.IsLower(previous)
            || Rune.GetUnicodeCategory(previous) == UnicodeCategory.DecimalDigitNumber
            || Rune.IsUpper(previous) && next is { } following && Rune.IsLower(following));

    static void AddWord(List<List<Rune>> words, List<Rune> word)
    {
        if (word.Count == 0)
            return;

        words.Add([..word]);
        word.Clear();
    }

    static void AppendWord(StringBuilder result, List<Rune> word, WordCase wordCase, int index)
    {
        for (var runeIndex = 0; runeIndex < word.Count; runeIndex++)
        {
            var rune = word[runeIndex];
            var converted = wordCase switch
                {
                    WordCase.Upper => Rune.ToUpperInvariant(rune),
                    WordCase.Pascal when runeIndex == 0 => Rune.ToUpperInvariant(rune),
                    WordCase.Camel when index > 0 && runeIndex == 0 => Rune.ToUpperInvariant(rune),
                    _ => Rune.ToLowerInvariant(rune)
                };

            result.Append(converted);
        }
    }

    enum WordCase
    {
        Lower,
        Upper,
        Camel,
        Pascal
    }
}