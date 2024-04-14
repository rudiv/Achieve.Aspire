using Achieve.Aspire.AzureProvisioning.RoleAssignment;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning.Authorization;
using Azure.ResourceManager.Authorization.Models;

namespace Achieve.Aspire.AzureProvisioning;

public static class RoleAssignmentExtensions
{
    public static IResourceBuilder<AzureRoleAssignmentResource> AddAzureRoleAssignment(this IDistributedApplicationBuilder builder,
        IResourceBuilder<AzureBicepResource> targetResource,
        IResourceBuilder<AzureManagedIdentityResource> managedIdentity,
        RoleDefinition roleDefinitionId)
    {
        var name = targetResource.Resource.Name + managedIdentity.Resource.Name + roleDefinitionId;
        var uniqueId = Helpers.StableGuid(name);

        var raBicep = new RoleAssignmentBicep(
            targetResource.GetOutput("").ValueExpression,
            roleDefinitionId,
            managedIdentity.Resource.PrincipalId.ValueExpression);
        
        

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
    
    
    public class AzureRoleAssignmentResource(string name, Action<ResourceModuleConstruct> configureConstruct) : AzureConstructResource(name, configureConstruct)
    {
    }
}