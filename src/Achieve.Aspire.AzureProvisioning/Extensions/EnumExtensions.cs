using System.Reflection;
using System.Runtime.Serialization;

namespace Achieve.Aspire.AzureProvisioning.Extensions;

/// <summary>
/// Provides extensions for use with enums.
/// </summary>
internal static class EnumExtensions
{
    /// <summary>
    /// Reads the value of an enum out of the attached <see cref="EnumMemberAttribute"/> attribute.
    /// </summary>
    /// <typeparam name="T">The enum.</typeparam>
    /// <param name="value">The value of the enum to pull the value for.</param>
    /// <returns></returns>
    internal static string GetValueFromEnumMember<T>(this T value) where T : Enum
    {
        var memberInfo = typeof(T).GetMember(value.ToString(),
            BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly);
        if (memberInfo.Length <= 0)
        {
            return value.ToString();
        }

        var attributes = memberInfo[0].GetCustomAttributes(typeof(EnumMemberAttribute), false);
        if (attributes.Length > 0)
        {
            var targetAttribute = (EnumMemberAttribute) attributes[0];
            if (targetAttribute is {Value: not null})
            {
                return targetAttribute.Value;
            }
        }

        return value.ToString();
    }
}
