using Achieve.Aspire.AzureProvisioning.Bicep.Authorization;
using Achieve.Aspire.AzureProvisioning.Bicep.Internal;
using Achieve.Aspire.AzureProvisioning.Bicep.KeyVault;
using Achieve.Aspire.AzureProvisioning.Resources;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning.Authorization;

namespace Achieve.Aspire.AzureProvisioning;

public static class RoleAssignmentExtensions
{
    public static IResourceBuilder<AzureRoleAssignmentResource>? AddAzureRoleAssignment(this IDistributedApplicationBuilder builder,
        IResourceBuilder<AzureConstructResource> targetConstructResource,
        IResourceBuilder<AzureManagedIdentityResource> managedIdentity,
        RoleDefinition roleDefinition)
    {
        // We should only create Role Assignments in Publish Mode
        if (!builder.ExecutionContext.IsPublishMode)
        {
            return default;
        }
        
        var bicepFileOutput = BicepFileOutput.GetAspireFileOutput();
        bicepFileOutput.AddParameter(new BicepParameter("resourceName", BicepSupportedType.String, Description: "The target resource."));
        bicepFileOutput.AddParameter(new BicepParameter("principalId", BicepSupportedType.String, Description: "The principal ID."));

        BicepResource scope;
        switch(targetConstructResource.Resource)
        {
            case AzureKeyVaultResource:
                scope = new KeyVaultResource().AsExisting(new BicepVariableValue("resourceName"));
                break;
            default:
                throw new NotSupportedException("The target resource type is not yet supported by Achieve.");
        }
        
        bicepFileOutput.AddResource(scope);
        bicepFileOutput.AddResource(new RoleAssignmentResource
        {
            Scope = scope,
            RoleDefinitionId = roleDefinition.ToString(),
            PrincipalId = new BicepVariableValue("principalId"),
            PrincipalType = RoleAssignmentPrincipalType.ServicePrincipal,
        });
        
        var name = "ra" + Helpers.StableIdentifier(targetConstructResource.Resource.Name + managedIdentity.Resource.Name + roleDefinition);
        var resource = new AzureRoleAssignmentResource(name, bicepFileOutput);
        return builder.AddResource(resource)
            .WithParameter("resourceName", targetConstructResource.GetOutput("name"))
            .WithParameter("principalId", managedIdentity.GetOutput("principalId"))
            .WithManifestPublishingCallback(resource.WriteToManifest);

        // Sigh. Guess what, it's internal :)))))))))))
        /*builder.AddAzureProvisioning();
        var configureConstruct = (ResourceModuleConstruct c) =>
        {
            //var id = new RoleAssignment(targetResource, roleDefinitionId, uniqueId, RoleManagementPrincipalType.ServicePrincipal);
        };
        var resource = new AzureRoleAssignmentResource(name, configureConstruct);
        return builder.AddResource(resource)
            .WithManifestPublishingCallback(resource.WriteToManifest);*/
    }
    
    public class AzureRoleAssignmentResource(string name, BicepFileOutput bicepFileOutput) : AchieveResource(name, bicepFileOutput)
    {
    }
}