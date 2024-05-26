using System.Text.Json;
using Achieve.Aspire.AzureProvisioning.Tests.Utils;
using Aspire.Hosting;
using Aspire.Hosting.Azure;
using Azure.ResourceManager.Models;
using Xunit.Abstractions;

namespace Achieve.Aspire.AzureProvisioning.Tests;

public class RoleAssignmentTests(ITestOutputHelper output)
{
    [Fact]
    public void AzureProvisionerIsAdded()
    {
      using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
      var id = builder.AddManagedIdentity("testid");
      var kv = builder.AddZtAzureKeyVault("kv", o => { });
      var _ = builder.AddAzureRoleAssignment(kv, id, KeyVaultRoles.CertificateUser);
      Assert.Contains(builder.Services, m => m.ServiceKey != null && m.ServiceKey as Type == typeof(AzureBicepResource));
    }
  
    [Fact]
    public async Task RoleAssignmentGeneratesCorrectlyForKeyVault()
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
                              scope: kv_88F2BAED
                              properties: {
                                  principalId: principalId
                                  roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions','db79e9a7-68ee-4b58-9aeb-b90e7c24fcba')
                                  principalType: 'ServicePrincipal'
                                }
                            }
                            
                            
                            """;
        Assert.Equal(expectedBicep, identityManifestBicep.BicepText);
    }
    
    [Fact]
    public async Task RoleAssignmentGeneratesCorrectlyForMaps()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        var id = builder.AddManagedIdentity("testid");
        var maps = builder.AddAzureMapsAccount("maps");
        var ra = builder.AddAzureRoleAssignment(maps, id, MapsRoles.DataReader);

        var identityManifestBicep = await ManifestUtils.GetManifestWithBicep(ra.Resource);
        
        var expectedManifest = """
                               {
                                 "type": "azure.bicep.v0",
                                 "path": "raE84B02F8.achieve.bicep",
                                 "params": {
                                   "principalId": "{testid.outputs.principalId}"
                                 }
                               }
                               """;
        Assert.Equal(expectedManifest, identityManifestBicep.ManifestNode.ToString());
        
        // TODO Remove the extraneous spaces
        var expectedBicep = """
                            targetScope = 'resourceGroup'
                            
                            @description('The location of the resource group.')
                            param location string = resourceGroup().location
                            
                            @description('The principal ID.')
                            param principalId string
                            
                            resource er_7930E06C 'Microsoft.Maps/accounts@2023-06-01' existing = {
                              name: 'maps'
                            }
                            
                            resource ra_FEE0F457 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
                              name: guid(er_7930E06C.id,subscriptionResourceId('Microsoft.Authorization/roleDefinitions','423170ca-a8f6-4b0f-8487-9e4eb8f49bfa'))
                              location: location
                              scope: er_7930E06C
                              properties: {
                                  principalId: principalId
                                  roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions','423170ca-a8f6-4b0f-8487-9e4eb8f49bfa')
                                  principalType: 'ServicePrincipal'
                                }
                            }
                            
                            
                            """;
        Assert.Equal(expectedBicep, identityManifestBicep.BicepText);
    }
    
    [Fact]
    public async Task DevelopmentRoleAssignmentGeneratesCorrectlyForMaps()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);
        var maps = builder.AddAzureMapsAccount("maps");
        var ra = builder.AddDevelopmentRoleAssignment(maps, MapsRoles.DataReader);

        var identityManifestBicep = await ManifestUtils.GetManifestWithBicep(ra.Resource);
        
        var expectedManifest = """
                               {
                                 "type": "azure.bicep.v0",
                                 "path": "ra978A101B.achieve.bicep"
                               }
                               """;
        Assert.Equal(expectedManifest, identityManifestBicep.ManifestNode.ToString());
        
        // TODO Remove the extraneous spaces
        var expectedBicep = """
                            targetScope = 'resourceGroup'
                            
                            @description('The location of the resource group.')
                            param location string = resourceGroup().location
                            
                            @description('(Aspire Provided) Principal ID')
                            param principalId string
                            
                            @description('(Aspire Provided) Principal Type')
                            param principalType string
                            
                            resource er_7930E06C 'Microsoft.Maps/accounts@2023-06-01' existing = {
                              name: 'maps'
                            }
                            
                            resource ra_FEE0F457 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
                              name: guid(er_7930E06C.id,subscriptionResourceId('Microsoft.Authorization/roleDefinitions','423170ca-a8f6-4b0f-8487-9e4eb8f49bfa'))
                              location: location
                              scope: er_7930E06C
                              properties: {
                                  principalId: principalId
                                  roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions','423170ca-a8f6-4b0f-8487-9e4eb8f49bfa')
                                  principalType: 'ServicePrincipal'
                                }
                            }
                            
                            
                            """;
        Assert.Equal(expectedBicep, identityManifestBicep.BicepText);
    }
}