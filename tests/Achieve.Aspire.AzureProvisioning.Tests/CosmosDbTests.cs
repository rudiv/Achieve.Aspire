using System.Text.Json;
using Achieve.Aspire.AzureProvisioning.Bicep.CosmosDb;
using Achieve.Aspire.AzureProvisioning.Tests.Utils;
using Aspire.Hosting;
using Azure.ResourceManager.Models;
using Xunit.Abstractions;

namespace Achieve.Aspire.AzureProvisioning.Tests;

public class CosmosDbTests(ITestOutputHelper output)
{
    [Fact]
    public async Task BasicCosmosDbGeneratesCorrectly()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        var id = builder.AddManagedIdentity("testid");
        var cosmos = builder.AddAzureCosmosDbNoSqlAccount("cosmos", acc =>
        {
          acc.AddDatabase("db", db =>
          {

          }).AddContainer("cn", cn =>
          {
            cn.PartitionKey = new CosmosDbSqlContainerPartitionKey("/id");
          });
        });

        var identityManifestBicep = await ManifestUtils.GetManifestWithBicep(cosmos.Resource);
        
        var expectedManifest = """
                               {
                                 "type": "azure.bicep.v0",
                                 "path": "cosmos.achieve.bicep"
                               }
                               """;
        Assert.Equal(expectedManifest, identityManifestBicep.ManifestNode.ToString());
        
        var expectedBicep = """
                            targetScope = 'resourceGroup'
                            
                            @description('The location of the resource group.')
                            param location string = resourceGroup().location
                            
                            resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2023-11-15' = {
                              name: 'cosmos${uniqueString(resourceGroup().id)}'
                              location: location
                              properties: {
                                databaseAccountOfferType: 'Standard'
                                backupPolicy: {
                                  type: 'Continuous'
                                  continuousModeProperties: {
                                    tier: 'Continuous7Days'
                                  }
                                }
                                capabilities: [
                                  {
                                    name: 'EnableServerless'
                                  }
                                ]
                                consistencyPolicy: {
                                  defaultConsistencyLevel: 'Session'
                                }
                                locations: [
                                  {
                                    failoverPriority: 0
                                    locationName: location
                                    isZoneRedundant: false
                                  }
                                ]
                                minimalTlsVersion: 'Tls12'
                                publicNetworkAccess: 'SecuredByPerimeter'
                              }
                            }
                            
                            resource db 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2023-11-15' = {
                              parent: cosmosDbAccount
                              name: 'db'
                              location: location
                              properties: {
                                resource: {
                                  id: 'db'
                                }
                              }
                            }
                            
                            resource cn 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-11-15' = {
                              parent: db
                              name: 'cn'
                              location: location
                              properties: {
                                resource: {
                                  id: 'cn'
                                  partitionKey: {
                                    kind: 'Hash'
                                    paths: [
                                      '/id'
                                    ]
                                  }
                                }
                              }
                            }
                            
                            output accountEndpoint string = cosmosDbAccount.properties.documentEndpoint
                            
                            """;
        Assert.Equal(expectedBicep, identityManifestBicep.BicepText);
    }
}