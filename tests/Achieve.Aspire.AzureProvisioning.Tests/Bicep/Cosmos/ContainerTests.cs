using Achieve.Aspire.AzureProvisioning.Bicep.CosmosDb;
using Xunit.Abstractions;

namespace Achieve.Aspire.AzureProvisioning.Tests.Bicep.Cosmos;

public class ContainerTests(ITestOutputHelper output)
{
    [Fact]
    public void ContainerPartitionKeyPathsAreGeneratedCorrectly()
    {
        var cosmosAccount = new CosmosDbAccountResource("test");
        var database = new CosmosDbSqlDatabaseResource(cosmosAccount, "testDb");
        var container = new CosmosDbSqlContainerResource(database, "testCn");

        // PartitionKey not defined
        Assert.Throws<InvalidOperationException>(() => container.ToBicepSyntax());

        container.PartitionKey = new CosmosDbSqlContainerPartitionKey("/id");

        var expected = """
                      resource testCn 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-11-15' = {
                        parent: testDb
                        name: 'testCn'
                        location: location
                        properties: {
                          resource: {
                            id: 'testCn'
                            partitionKey: {
                              kind: 'Hash'
                              paths: [
                                '/id'
                              ]
                            }
                          }
                        }
                      }
                      """;
        Assert.Equal(expected, container.ToBicepSyntax().ToString());

        container.PartitionKey = new CosmosDbSqlContainerPartitionKey("/id", "/type");
        
        output.WriteLine(container.ToBicepSyntax().ToString());
        
        expected = """
                   resource testCn 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-11-15' = {
                     parent: testDb
                     name: 'testCn'
                     location: location
                     properties: {
                       resource: {
                         id: 'testCn'
                         partitionKey: {
                           kind: 'MultiHash'
                           version: 2
                           paths: [
                             '/id'
                             '/type'
                           ]
                         }
                       }
                     }
                   }
                   """;
        Assert.Equal(expected, container.ToBicepSyntax().ToString());
    }
}