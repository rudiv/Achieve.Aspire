using Microsoft.WindowsAzure.ResourceStack.Common.Extensions;

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
        UppercaseLetter = 1,
        /// <summary>
        /// Reflects a lowercase letter character.
        /// </summary>
        LowercaseLetter = 2,
        /// <summary>
        /// Reflects an underscore character.
        /// </summary>
        Underscore = 4,
        /// <summary>
        /// Reflects a hyphen character.
        /// </summary>
        Hyphen = 8,
        /// <summary>
        /// Reflects a whitespace character.
        /// </summary>
        Whitespace = 16,
        /// <summary>
        /// Reflects a number.
        /// </summary>
        Number = 32,
        /// <summary>
        /// Reflects a period.
        /// </summary>
        Period = 64,
        /// <summary>
        /// Reflects either open or close parentheses.
        /// </summary>
        Parentheses = 128,
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
        Any = UppercaseLetter | LowercaseLetter | Number | Underscore | Hyphen | Whitespace | Period | Parentheses
    }

    /// <summary>
    /// Validates that a given string matches the specified naming constraints.
    /// </summary>
    /// <param name="str">The string to evaluate.</param>
    /// <param name="minLength">The minimum allowed length of the string.</param>
    /// <param name="maxLength">The maximum allowed length of the string.</param>
    /// <param name="contains">The types of characters allowed in the string.</param>
    /// <param name="doesNotContain">The types of characters not allowed in the string.</param>
    /// <param name="startsWith">The types of characters allowed for the string to start with, if any.</param>
    /// <param name="doesNotStartWith">The types of characters the string is not allowed to start with, if any.</param>
    /// <param name="endsWith">The types of characters allowed for the string to end with, if any.</param>
    /// <param name="doesNotEndWith">The types of characters the string is not allowed to end with, if any.</param>
    /// <returns>True if the string matches the provided constraints; otherwise false.</returns>
    public static bool MatchesConstraints(this string str, int? minLength = null, int? maxLength = null, CharacterClass? contains = null, CharacterClass? doesNotContain = null, CharacterClass? startsWith = null, CharacterClass? doesNotStartWith = null, CharacterClass? endsWith = null, CharacterClass? doesNotEndWith = null)
    {
        //Validate the length constraints
        if ((minLength is not null && str.Length < minLength) || (maxLength is not null && str.Length > maxLength))
            return false;

        //If there's no string length and we've otherwise met the length constraints, skip the remaining checks
        if (str.Length == 0)
            return true;

        return ValidateConstraint(str.First().ToString(), startsWith) &&
               ValidateConstraint(str.First().ToString(), doesNotStartWith, true) &&
               ValidateConstraint(str.Last().ToString(), endsWith) &&
               ValidateConstraint(str.Last().ToString(), doesNotEndWith, true) &&
               ValidateConstraint(str, contains) &&
               ValidateConstraint(str, doesNotContain, true);

        bool ValidateConstraint(string value, CharacterClass? constraint, bool checkAsNot = false)
        {
            if (constraint == null)
                return true;
            
            var allowedChars = new HashSet<char>();

            if (((CharacterClass)constraint).HasFlag(CharacterClass.UppercaseLetter))
                allowedChars.UnionWith("ABCDEFGHIJKLMNOPQRSTUVWXYZ");

            if (((CharacterClass)constraint).HasFlag(CharacterClass.LowercaseLetter))
                allowedChars.UnionWith("abcdefghijklmnopqrstuvwxyz");

            if (((CharacterClass)constraint).HasFlag(CharacterClass.Number))
                allowedChars.UnionWith("0123456789");

            if (((CharacterClass)constraint).HasFlag(CharacterClass.Underscore))
                allowedChars.Add('_');

            if (((CharacterClass)constraint).HasFlag(CharacterClass.Hyphen))
                allowedChars.Add('-');

            if (((CharacterClass)constraint).HasFlag(CharacterClass.Period))
                allowedChars.Add('.');

            if (((CharacterClass) constraint).HasFlag(CharacterClass.Parentheses)) 
                allowedChars.UnionWith(['(', ')']);

            return value.All(c => allowedChars.Contains(c) == !checkAsNot);
        }
    }
}
