using System.Text.Json;
using Achieve.Aspire.AzureProvisioning.Tests.Utils;
using Aspire.Hosting;
using Azure.ResourceManager.Models;
using Xunit.Abstractions;

namespace Achieve.Aspire.AzureProvisioning.Tests;

public class RoleAssignmentTests(ITestOutputHelper output)
{
    [Fact]
    public async Task RoleAssignmentGeneratesCorrectly()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        var id = builder.AddManagedIdentity("testid");
        var kv = builder.AddZtAzureKeyVault("kv", o => { });
        var ra = builder.AddAzureRoleAssignment(kv, id, KeyVaultRoles.CertificateUser);

        var identityManifestBicep = await ManifestUtils.GetManifestWithBicep(ra.Resource);
        var expectedManifest = """
                               {
                                 "type": "azure.bicep.v0",
                                 "path": "ra27911E3B.achieve.bicep",
                                 "params": {
                                   "resourceName": "{kv.outputs.name}",
                                   "principalId": "{testid.outputs.principalId}"
                                 }
                               }
                               """;
        Assert.Equal(expectedManifest, identityManifestBicep.ManifestNode.ToString());
        output.WriteLine(identityManifestBicep.BicepText);
        
        // TODO Remove the extraneous spaces
        var expectedBicep = """
                            targetScope = 'resourceGroup'
                            
                            @description('The location of the resource group.')
                            param location string = resourceGroup().location
                            
                            @description('The target resource.')
                            param resourceName string
                            
                            @description('The principal ID.')
                            param principalId string
                            
                            resource kv_88F2BAED 'Microsoft.KeyVault/vaults@2022-07-01' existing = {
                              name: resourceName
                            }
                            
                            resource ra_6E25D339 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
                              name: guid(kv_88F2BAED.id,subscriptionResourceId('Microsoft.Authorization/roleDefinitions','db79e9a7-68ee-4b58-9aeb-b90e7c24fcba'))
                              location: location
                            }
                            
                            
                            """;
        Assert.Equal(expectedBicep, identityManifestBicep.BicepText);
    }
}