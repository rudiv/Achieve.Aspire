

using System.Diagnostics.CodeAnalysis;
using Microsoft.SRM;

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
    public enum CharacterClass
    {
        /// <summary>
        /// Reflects an uppercase letter character.
        /// </summary>
        UppercaseLetter = 0b_1,
        /// <summary>
        /// Reflects a lowercase letter character.
        /// </summary>
        LowercaseLetter = 0b_10,
        /// <summary>
        /// Reflects an underscore character.
        /// </summary>
        Underscore = 0b_100,
        /// <summary>
        /// Reflects a hyphen character.
        /// </summary>
        Hyphen = 0b_1000,
        /// <summary>
        /// Reflects a whitespace character.
        /// </summary>
        Whitespace = 0b_10000,
        /// <summary>
        /// Reflects a number.
        /// </summary>
        Number = 0b_100000,
        /// <summary>
        /// Reflects an alphabetic character.
        /// </summary>
        Alphabetic = UppercaseLetter | LowercaseLetter,
        /// <summary>
        /// Reflects an alphanumeric character.
        /// </summary>
        Alphanumeric = Alphabetic | Number,
        /// <summary>
        /// Reflects any character as being valid.
        /// </summary>
        Any = UppercaseLetter | LowercaseLetter | Number | Underscore | Hyphen | Whitespace
    }

    /// <summary>
    /// Validates that a given string matches the specified naming constraints.
    /// </summary>
    /// <param name="str">The string to evaluate.</param>
    /// <param name="minLength">The minimum allowed length of the string.</param>
    /// <param name="maxLength">The maximum allowed length of the string.</param>
    /// <param name="contains">The types of characters allowed in the string.</param>
    /// <param name="startsWith">The types of characters allowed for the string to start with, if any.</param>
    /// <param name="endsWith">The types of characters allowed for the string to end with, if any.</param>
    /// <returns>True if the string matches the provided constraints; otherwise false.</returns>
    public static bool MatchesConstraints(this string str, int? minLength = null, int? maxLength = null, CharacterClass? contains = null, CharacterClass? startsWith = null, CharacterClass? endsWith = null)
    {
        if ((minLength is not null && str.Length < minLength) || (maxLength is not null && str.Length > maxLength))
            return false;

        //Validate that the string starts with the specified constraint
        if (startsWith is not null)
        {
            if (str.Length > 0)
            {
                var startingCharacter = str.First();
                var startingCharacterResult =
                    MatchesConstraints(startingCharacter.ToString(), 1, 1, (CharacterClass) startsWith);
                if (!startingCharacterResult)
                    return false;
            }
        }

        //Validate that the string ends with the specified constraint
        if (endsWith is not null)
        {
            if (str.Length > 0)
            {
                var endingCharacter = str.Last();
                var endingCharacterResult =
                    MatchesConstraints(endingCharacter.ToString(), 1, 1, (CharacterClass) endsWith);
                if (!endingCharacterResult)
                    return false;
            }
        }

        if (contains is null)
            return true;

        if (((CharacterClass)contains).HasFlag(CharacterClass.Any))
            return true;
        
        //Create a set of allowed characters based on the flags
        var allowedChars = new HashSet<char>();
        
        if (((CharacterClass)contains).HasFlag(CharacterClass.UppercaseLetter))
            allowedChars.UnionWith("ABCDEFGHIJKLMNOPQRSTUVWXYZ");

        if (((CharacterClass)contains).HasFlag(CharacterClass.LowercaseLetter))
            allowedChars.UnionWith("abcdefghijklmnopqrstuvwxyz");

        if (((CharacterClass)contains).HasFlag(CharacterClass.Number))
            allowedChars.UnionWith("0123456789");

        if (((CharacterClass)contains).HasFlag(CharacterClass.Underscore))
            allowedChars.Add('_');

        if (((CharacterClass)contains).HasFlag(CharacterClass.Hyphen))
            allowedChars.Add('-');
        
        // Check each character in the input string
        return str.All(c => allowedChars.Contains(c));
    }
}
