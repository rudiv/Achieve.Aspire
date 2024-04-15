using Achieve.Aspire.AzureProvisioning.Bicep.Internal;

namespace Achieve.Aspire.AzureProvisioning.Bicep.Authorization;

public class RoleAssignmentResource : BicepResource
{
    private const string ResourceType = "Microsoft.Authorization/roleAssignments@2022-04-01";
    private const string RoleDefinitionResourceType = "Microsoft.Authorization/roleDefinitions";
    private const string PropertyRoleDefinitionId = "roleDefinitionId";
    private const string PropertyPrincipalId = "principalId";
    private const string PropertyPrincipalType = "principalType";
        
    public BicepResource Scope { get; set; }
    public string RoleDefinitionId { get; set; }
    public BicepValue PrincipalId { get; set; }
    public RoleAssignmentPrincipalType PrincipalType { get; set; }

    public RoleAssignmentResource() : base(ResourceType)
    {
    }
    
    public override void Construct()
    {
        Name = "ra_" + Helpers.StableIdentifier(Scope.Name + RoleDefinitionId + PrincipalId.ToBicepSyntax() + PrincipalType);
        var roleDefId = new BicepFunctionCallValue("subscriptionResourceId",
            new BicepStringValue(RoleDefinitionResourceType),
            new BicepStringValue(RoleDefinitionId));
        Body.Add(new BicepResourceProperty(BicepResourceProperties.Name,
            new BicepFunctionCallValue("guid", 
                new BicepPropertyAccessValue(new BicepVariableValue(Scope.Name), "id"),
                roleDefId)));
        Body.Add(new BicepResourceProperty(BicepResourceProperties.Location, new BicepVariableValue("location")));

        var propertyBag = new BicepResourcePropertyBag(BicepResourceProperties.Properties, 2);
        propertyBag.AddProperty(new BicepResourceProperty(PropertyPrincipalId, PrincipalId));
        propertyBag.AddProperty(new BicepResourceProperty(PropertyRoleDefinitionId, roleDefId));
        propertyBag.AddProperty(new BicepResourceProperty(PropertyPrincipalType, new BicepStringValue(PrincipalType.ToString())));
    }
}

public enum RoleAssignmentPrincipalType
{
    User,
    Group,
    ServicePrincipal
}