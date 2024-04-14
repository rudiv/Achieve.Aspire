using Bicep.Core.Syntax;

namespace Achieve.Aspire.AzureProvisioning.Bicep;

internal class BicepResource(string Name, string Type) : IBicepSyntaxGenerator
{
    public SyntaxBase ToBicepSyntax()
    {
        
    }
}

internal enum BicepSupportedResourceType
{
    RoleAssignment
}

internal static class BicepResourceTypeMap
{
    public static string GetResourceType(BicepSupportedResourceType resourceType)
    {
        return resourceType switch
        {
            BicepSupportedResourceType.RoleAssignment => "Microsoft.Authorization/roleAssignments@2022-04-01",
            _ => throw new ArgumentOutOfRangeException(nameof(resourceType), resourceType, null)
        };
    }
}