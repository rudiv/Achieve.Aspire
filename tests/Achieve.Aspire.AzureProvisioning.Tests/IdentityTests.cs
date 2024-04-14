using System.Text.Json;
using Achieve.Aspire.AzureProvisioning.Tests.Utils;
using Aspire.Hosting;
using Azure.ResourceManager.Models;
using Xunit.Abstractions;

namespace Achieve.Aspire.AzureProvisioning.Tests;

public class IdentityTests(ITestOutputHelper output)
{
    [Fact]
    public async Task ManagedIdentityGetsAddedForDeployment()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var id = builder.AddManagedIdentity("testid");
        
        Assert.Equal("testid", id.Resource.Name);
        Assert.Equal("{testid.outputs.principalId}", id.Resource.PrincipalId.ValueExpression);
        
        var identityManifestBicep = await ManifestUtils.GetManifestWithBicep(id.Resource);
        var expectedManifest = """
                               {
                                 "type": "azure.bicep.v0",
                                 "path": "testid.module.bicep"
                               }
                               """;
        Assert.Equal(expectedManifest, identityManifestBicep.ManifestNode.ToString());
        
        var expectedBicep = """
                            targetScope = 'resourceGroup'
                            
                            @description('')
                            param location string = resourceGroup().location
                            
                            
                            resource userAssignedIdentity_U5RwYXf18 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
                              name: toLower(take('testid${uniqueString(resourceGroup().id)}', 24))
                              location: location
                              properties: {
                              }
                            }
                            
                            output principalId string = userAssignedIdentity_U5RwYXf18.properties.principalId
                            output clientId string = userAssignedIdentity_U5RwYXf18.properties.clientId
                            output resourceId string = userAssignedIdentity_U5RwYXf18.id
                            
                            """;
        Assert.Equal(expectedBicep, identityManifestBicep.BicepText);
    }

    [Fact]
    public async Task ProjectHasUaiManifestNode()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var id = builder.AddManagedIdentity("testid");
        var proj = builder.AddProject("test", @"../../../Achieve.Aspire.AzureProvisioning.Tests.csproj")
            .WithManagedIdentity("TESTID", id);
        
        var projectManifest = await ManifestUtils.GetManifest(proj.Resource);
        Assert.NotNull(projectManifest["UserAssignedIdentities"]);
        Assert.Equal(JsonValueKind.Array, projectManifest["UserAssignedIdentities"]!.GetValueKind());
        Assert.Equal(id.GetOutput("clientId").ValueExpression, projectManifest["UserAssignedIdentities"]![0]!["clientId"]!.ToString());
        Assert.Equal(id.GetOutput("resourceId").ValueExpression, projectManifest["UserAssignedIdentities"]![0]!["resourceId"]!.ToString());
        Assert.Equal("TESTID", projectManifest["UserAssignedIdentities"]![0]!["env"]!.ToString());
    }
}