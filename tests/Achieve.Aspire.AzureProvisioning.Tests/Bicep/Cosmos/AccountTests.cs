using Achieve.Aspire.AzureProvisioning.Bicep.CosmosDb;
using Xunit.Abstractions;

namespace Achieve.Aspire.AzureProvisioning.Tests.Bicep.Cosmos;

public class AccountTests(ITestOutputHelper output)
{
    [Fact]
    public void CreateDefaultAccount()
    {
        var cosmosAccount = new CosmosDbAccountResource("test");

        var expected = """
                      resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2023-11-15' = {
                        name: 'test${uniqueString(resourceGroup().id)}'
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
                      """;
        Assert.Equal(expected, cosmosAccount.ToBicepSyntax().ToString());
    }

    [Fact]
    public void CosmosAccountDevelopmentDefaults()
    {
      var cosmosAccount = new CosmosDbAccountResource("test");
      cosmosAccount.WithDevelopmentDefaults();

      var expected = """
                     resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2023-11-15' = {
                       name: 'test${uniqueString(resourceGroup().id)}'
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
                         networkAclBypass: 'AzureServices'
                         publicNetworkAccess: 'Enabled'
                       }
                     }
                     """;
      
      Assert.Equal(expected, cosmosAccount.ToBicepSyntax().ToString());
    }
}