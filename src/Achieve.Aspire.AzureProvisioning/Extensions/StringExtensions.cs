

namespace Achieve.Aspire.AzureProvisioning.Extensions;

/// <summary>
/// Provides extensions for use with strings.
/// </summary>
internal static class StringExtensions
{
    /// <summary>
    /// Denotes classes of character types.
    /// </summary>
    [Flags]
    internal enum CharacterClass
    {
        UppercaseLetter = 0b_000001,
        LowercaseLetter = 0b_000010,
        Underscore = 0b_000100,
        Hyphen = 0b_001000,
        Whitespace = 0b_010000,
        Number = 0b_100000,
        Alphabetic = UppercaseLetter | LowercaseLetter,
        Alphanumeric = Alphabetic | Number, 
        Any = UppercaseLetter | LowercaseLetter | Number | Underscore | Hyphen | Whitespace
    }

    /// <summary>
    /// Validates that a given string matches the specified naming constraints.
    /// </summary>
    /// <param name="str">The string to evaluate.</param>
    /// <param name="minLength">The minimum allowed length of the string.</param>
    /// <param name="maxLength">The maximum allowed length of the string.</param>
    /// <param name="characterClasses">The types of characters allowed in the string.</param>
    /// <returns>True if the string matches the provided constraints; otherwise false.</returns>
    public static bool MatchesConstraints(this string str, int? minLength, int? maxLength, CharacterClass characterClasses)
    {
        if ((minLength != null && str.Length < minLength) || (maxLength != null && str.Length > maxLength))
            return false;

        if (characterClasses.HasFlag(CharacterClass.Any))
            return true;
        
        //Create a set of allowed characters based on the flags
        var allowedChars = new HashSet<char>();
        
        if (characterClasses.HasFlag(CharacterClass.UppercaseLetter))
            allowedChars.UnionWith("ABCDEFGHIJKLMNOPQRSTUVWXYZ");

        if (characterClasses.HasFlag(CharacterClass.LowercaseLetter))
            allowedChars.UnionWith("abcdefghijklmnopqrstuvwxyz");

        if (characterClasses.HasFlag(CharacterClass.Number))
            allowedChars.UnionWith("0123456789");

        if (characterClasses.HasFlag(CharacterClass.Underscore))
            allowedChars.Add('_');

        if (characterClasses.HasFlag(CharacterClass.Hyphen))
            allowedChars.Add('-');
        
        // Check each character in the input string
        return str.All(c => allowedChars.Contains(c));
    }
}
