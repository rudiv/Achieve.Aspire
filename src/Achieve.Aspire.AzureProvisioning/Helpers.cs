using System.Security.Cryptography;
using System.Text;

namespace Achieve.Aspire.AzureProvisioning;

public static class Helpers
{
    public static Guid StableGuid(string name) => new(MD5.HashData(Encoding.UTF8.GetBytes(name)));

    public static string StableIdentifier(string name) => Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(name)))[..8];
}