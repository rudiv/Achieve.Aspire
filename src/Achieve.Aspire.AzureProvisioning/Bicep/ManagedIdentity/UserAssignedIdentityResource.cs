using Achieve.Aspire.AzureProvisioning.Bicep.Internal;
using Bicep.Core.Syntax;

namespace Achieve.Aspire.AzureProvisioning.ManagedIdentity;

public class UserAssignedIdentityResource : BicepResource
{
    private const string ResourceType = "Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31";

    public string PublicName { get; set; }
    
    public UserAssignedIdentityResource(string publicName) : base(ResourceType)
    {
        Name = "uai_" + Helpers.StableIdentifier(publicName);
        PublicName = publicName;
    }
    
    public override void Construct()
    {
        Body.Add(new BicepResourceProperty(BicepResourceProperties.Name,
            new BicepFunctionCallValue("toLower", 
                new BicepFunctionCallValue("take", 
                    new BicepInterpolatedString().Str(Name)
                        .Exp(new BicepFunctionCallValue("uniqueString", new BicepPropertyAccessValue(new BicepFunctionCallValue("resourceGroup"), "id")))),
                    new BicepIntValue(24))));
        Body.Add(new BicepResourceProperty(BicepResourceProperties.Location, new BicepVariableValue("location")));
    }
}