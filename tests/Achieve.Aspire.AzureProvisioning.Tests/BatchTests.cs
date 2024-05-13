using Achieve.Aspire.AzureProvisioning.Bicep.Batch;
using Achieve.Aspire.AzureProvisioning.Tests.Utils;
using Aspire.Hosting;
using Aspire.Hosting.Azure;
using Xunit.Abstractions;

namespace Achieve.Aspire.AzureProvisioning.Tests;

public class BatchTests(ITestOutputHelper output)
{
    [Fact]
    public void AzureProvisionerIsAdded()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        var cosmos = builder.AddAzureBatchAccount("batch", acc => { });
        Assert.Contains(builder.Services,
            m => m.ServiceKey != null && m.ServiceKey as Type == typeof(AzureBicepResource));
    }

    [Fact]
    public async Task BasicBatchGeneratesCorrectly()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        //var id = builder.AddManagedIdentity("testid");
        var batch = builder.AddAzureBatchAccount("batch", acc =>
        {
            acc.Resource.Identity = new BatchAccountIdentity
            {
                Type = BatchAccountIdentityType.SystemAssigned
            };
            acc.Resource.Properties = new BatchAccountCreatePropertiesOrBatchAccountProperties
            {
                Encryption = new EncryptionProperties
                {
                    KeySource = EncryptionKeySource.Batch
                },
                AutoStorage = new AutoStorageBasePropertiesOrAutoStorageProperties
                {
                    AuthenticationMode = AutoStorageAuthenticationMode.BatchAccountManagedIdentity,
                    NodeIdentityReference = new ComputeNodeIdentityReference
                    {
                        ResourceId = "uai"
                    },
                    StorageAccountId = "storage"
                },
                AllowedAuthenticationModes = [AllowedAuthenticationMode.Entra],
                PoolAllocationMode = PoolAllocationMode.BatchService,
                PublicNetworkAccess = PublicNetworkAccess.Disabled,
                KeyVaultReference = new KeyVaultReference
                {
                    Id = "kv",
                    Url = "https://kv"
                }
            };
            acc.AddCertificate("cert", certificate =>
            {
                certificate.Data = "abc123";
                certificate.Format = CertificateFormat.Pfx;
                certificate.Password = "password";
                certificate.Thumbprint = "aaa";
            });
        });

        var batchManifestBicep = await ManifestUtils.GetManifestWithBicep(batch.Resource);
        
        const string expectedManifest = """
                               {
                                 "type": "azure.bicep.v0",
                                 "path": "batch.achieve.bicep"
                               }
                               """;
        // var batchManifestNode = batchManifestBicep.ManifestNode.ToString();
        // Assert.Equal(expectedManifest, batchManifestNode);
        
        Assert.Equal(expectedManifest, batchManifestBicep.ManifestNode.ToString());

        const string expectedBicep = """
                            targetScope = 'resourceGroup'
                            
                            @description('The location of the resource group.')
                            param location string = resourceGroup().location
                            
                            resource batchAccount 'Microsoft.Batch/batchAccounts@2023-11-01' = {
                              name: 'batch${uniqueString(resourceGroup().id)}'
                              location: location
                              properties: {
                                allowedAuthenticationModes: [
                                  'AAD'
                                ]
                                poolAllocationMode: 'BatchService'
                                publicNetworkAccess: 'Disabled'
                              }
                            }
                            
                            resource cert 'Microsoft.Batch/batchAccounts/certificates@2023-11-01' = {
                              parent: batchAccount
                              name: 'cert'
                              properties: {
                                data: 'abc123'
                                format: 'Pfx'
                                password: 'password'
                                thumbprint: 'aaa'
                                thumbprintAlgorithm: 'SHA1'
                              }
                            }
                            """;
        var manifestBicep = batchManifestBicep.BicepText;
        Assert.Equal(expectedBicep.Trim(), manifestBicep.Trim());
    }
}
