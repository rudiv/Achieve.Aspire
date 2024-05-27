using System.Text.Json;
using Achieve.Aspire.AzureProvisioning.Bicep.CosmosDb;
using Achieve.Aspire.AzureProvisioning.Tests.Utils;
using Aspire.Hosting;
using Aspire.Hosting.Azure;
using Azure.ResourceManager.Models;
using Xunit.Abstractions;

namespace Achieve.Aspire.AzureProvisioning.Tests;

public class CosmosDbTests(ITestOutputHelper output)
{
  [Fact]
  public void AzureProvisionerIsAdded()
  {
    using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
    var _ = builder.AddAzureCosmosDbNoSqlAccount("cosmos", acc => {});
    Assert.Contains(builder.Services, m => m.ServiceKey != null && m.ServiceKey as Type == typeof(AzureBicepResource));
  }
  
    [Fact]
    public async Task BasicCosmosDbGeneratesCorrectly()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        var id = builder.AddManagedIdentity("testid");
        var cosmos = builder.AddAzureCosmosDbNoSqlAccount("cosmos", acc =>
        {
          acc.AddDatabase("db")
            .AddContainer("cn", cn =>
              {
                cn.PartitionKey = new CosmosDbSqlContainerPartitionKey("/id");
              });
        });

        var cosmosManifestBicep = await ManifestUtils.GetManifestWithBicep(cosmos.Resource);
        
        var expectedManifest = """
                               {
                                 "type": "azure.bicep.v0",
                                 "connectionString": "{cosmos.outputs.accountEndpoint}",
                                 "path": "cosmos.achieve.bicep"
                               }
                               """;
        Assert.Equal(expectedManifest, cosmosManifestBicep.ManifestNode.ToString());
        
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
        Assert.Equal(expectedBicep, cosmosManifestBicep.BicepText);
    }

    [Fact]
    public async Task CosmosAccountWithRbacGeneratesCorrectly()
    {
      using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
      var id = builder.AddManagedIdentity("testid");
      var cosmos = builder.AddAzureCosmosDbNoSqlAccount("cosmos", acc =>
      {
        var db = acc.AddDatabase("db");
        var conn = db.AddContainer("cn", cn =>
        {
          cn.PartitionKey = new CosmosDbSqlContainerPartitionKey("/id");
        });
        acc.WithDevelopmentGlobalAccess();
        acc.WithRoleAssignment(db.Resource, id, CosmosDbSqlBuiltInRole.Reader);
        acc.WithRoleAssignment(conn, id, CosmosDbSqlBuiltInRole.Contributor);
      });
      

      var cosmosManifestBicep = await ManifestUtils.GetManifestWithBicep(cosmos.Resource);
   
      var expectedManifest = """
                             {
                               "type": "azure.bicep.v0",
                               "connectionString": "{cosmos.outputs.accountEndpoint}",
                               "path": "cosmos.achieve.bicep",
                               "params": {
                                 "principalId": "",
                                 "testidPrincipal": "{testid.outputs.principalId}"
                               }
                             }
                             """;
      Assert.Equal(expectedManifest, cosmosManifestBicep.ManifestNode.ToString());

      var expected = """
                     targetScope = 'resourceGroup'

                     @description('The location of the resource group.')
                     param location string = resourceGroup().location

                     param principalId string

                     param testidPrincipal string

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

                     resource developmentAccess 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2023-11-15' = {
                       parent: cosmosDbAccount
                       name: guid(cosmosDbAccount.id,'developmentAccess')
                       properties: {
                         roleDefinitionId: resourceId('Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions',cosmosDbAccount.name,'00000000-0000-0000-0000-000000000002')
                         scope: cosmosDbAccount.id
                         principalId: principalId
                       }
                     }

                     resource testidRa_B9899A8C 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2023-11-15' = {
                       parent: cosmosDbAccount
                       name: guid(cosmosDbAccount.id,'testidRa_B9899A8C')
                       properties: {
                         roleDefinitionId: resourceId('Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions',cosmosDbAccount.name,'00000000-0000-0000-0000-000000000001')
                         scope: '${cosmosDbAccount.id}/dbs/${db.name}'
                         principalId: testidPrincipal
                       }
                     }

                     resource testidRa_BD6A548A 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2023-11-15' = {
                       parent: cosmosDbAccount
                       name: guid(cosmosDbAccount.id,'testidRa_BD6A548A')
                       properties: {
                         roleDefinitionId: resourceId('Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions',cosmosDbAccount.name,'00000000-0000-0000-0000-000000000002')
                         scope: '${cosmosDbAccount.id}/dbs/${db.name}/colls/${cn.name}'
                         principalId: testidPrincipal
                       }
                     }

                     output accountEndpoint string = cosmosDbAccount.properties.documentEndpoint

                     """;
      
      Assert.Equal(expected, cosmosManifestBicep.BicepText);
    }
}