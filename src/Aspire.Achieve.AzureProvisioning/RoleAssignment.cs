using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning.Authorization;
using Azure.ResourceManager.Authorization.Models;

namespace Aspire.Achieve.AzureProvisioning;

public static class RoleAssignmentExtensions
{
    public static IResourceBuilder<AzureRoleAssignmentResource> AddAzureRoleAssignment(this IDistributedApplicationBuilder builder,
        IResourceBuilder<Resource> targetResource,
        IResourceBuilder<AzureManagedIdentityResource> managedIdentity,
        RoleDefinition roleDefinitionId)
    {
        var name = targetResource.Resource.Name + managedIdentity.Resource.Name + roleDefinitionId;
        var uniqueId = Helpers.StableGuid(name);

        builder.AddAzureProvisioning();
        var configureConstruct = (ResourceModuleConstruct c) =>
        {
            //var id = new RoleAssignment(targetResource, roleDefinitionId, uniqueId, RoleManagementPrincipalType.ServicePrincipal);
        };
        
        var resource = new AzureRoleAssignmentResource(name, configureConstruct);
        
        return builder.AddResource(resource)
            .WithManifestPublishingCallback(resource.WriteToManifest);
    }
    
    
    public class AzureRoleAssignmentResource(string name, Action<ResourceModuleConstruct> configureConstruct) : AzureConstructResource(name, configureConstruct)
    {
    }
}