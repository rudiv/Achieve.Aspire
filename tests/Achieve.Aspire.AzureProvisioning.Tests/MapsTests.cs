using Achieve.Aspire.AzureProvisioning.Bicep.Batch;
using Achieve.Aspire.AzureProvisioning.Bicep.Internal;
using Achieve.Aspire.AzureProvisioning.Resources;
using Achieve.Aspire.AzureProvisioning.Tests.Utils;
using Aspire.Hosting;
using Aspire.Hosting.Azure;
using Xunit.Abstractions;

namespace Achieve.Aspire.AzureProvisioning.Tests;

public class MapsTests(ITestOutputHelper output)
{
    [Fact]
    public void AzureProvisionerIsAdded()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        var _ = builder.AddAzureMapsAccount("maps", acc => { });
        Assert.Contains(builder.Services,
            m => m.ServiceKey != null && m.ServiceKey as Type == typeof(AzureBicepResource));
    }

    [Fact]
    public async Task MapsGeneratesCorrectly()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        //var id = builder.AddManagedIdentity("testid");
        var batch = builder.AddAzureMapsAccount("maps");

        var batchManifestBicep = await ManifestUtils.GetManifestWithBicep(batch.Resource);
        
        const string expectedManifest = """
                               {
                                 "type": "azure.bicep.v0",
                                 "path": "maps.achieve.bicep"
                               }
                               """;
        
        Assert.Equal(expectedManifest, batchManifestBicep.ManifestNode.ToString());

        const string expectedBicep = """
                            targetScope = 'resourceGroup'
                            
                            @description('The location of the resource group.')
                            param location string = resourceGroup().location
                            
                            resource mapsAccount 'Microsoft.Maps/accounts@2023-06-01' = {
                              name: 'maps${uniqueString(resourceGroup().id)}'
                              location: location
                              kind: 'Gen2'
                              sku: {
                                name: 'G2'
                              }
                              properties: {
                                disableLocalAuth: true
                              }
                            }
                            
                            output clientId string = mapsAccount.properties.uniqueId
                            """;
        var manifestBicep = batchManifestBicep.BicepText;
        Assert.Equal(expectedBicep.Trim(), manifestBicep.Trim());
    }
    
    [Fact]
    public async Task MapsCanBeAsExisting()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        var batch = builder.AddAzureMapsAccount("maps");

        Assert.NotNull(batch.Resource.UnderlyingResource);
        var asExistingResource = batch.Resource.UnderlyingResource.AsExisting();
        
        // This bit would never be done in the real world, just testing magic
        var bicepFileOutput = BicepFileOutput.GetAspireFileOutput();
        bicepFileOutput.AddResource(asExistingResource);
        var bicep = bicepFileOutput.ToBicep().ToString();

        const string expectedBicep = """
                                     targetScope = 'resourceGroup'
                                     
                                     @description('The location of the resource group.')
                                     param location string = resourceGroup().location
                                     
                                     resource er_7930E06C 'Microsoft.Maps/accounts@2023-06-01' existing = {
                                       name: 'maps'
                                     }
                                     """;
        Assert.Equal(expectedBicep.Trim(), bicep.Trim());
    }
}
