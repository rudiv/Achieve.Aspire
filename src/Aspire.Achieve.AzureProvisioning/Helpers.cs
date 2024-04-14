using System.Security.Cryptography;
using System.Text;

namespace Aspire.Achieve.AzureProvisioning;

public static class Helpers
{
    public static Guid StableGuid(string name)
    {
        return new Guid(MD5.HashData(Encoding.UTF8.GetBytes(name)));
    }
}