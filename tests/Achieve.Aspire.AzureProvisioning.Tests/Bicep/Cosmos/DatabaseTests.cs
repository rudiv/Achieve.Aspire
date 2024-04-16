using Achieve.Aspire.AzureProvisioning.Bicep.CosmosDb;
using Xunit.Abstractions;

namespace Achieve.Aspire.AzureProvisioning.Tests.Bicep.Cosmos;

public class DatabaseTests(ITestOutputHelper output)
{
    [Fact]
    public void CreateSimpleDatabase()
    {
        var cosmosAccount = new CosmosDbAccountResource("test");
        var database = new CosmosDbSqlDatabaseResource(cosmosAccount, "testDb");

        var expected = """
                      resource testDb 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2023-11-15' = {
                        parent: cosmosDbAccount
                        name: 'testDb'
                        location: location
                        properties: {
                          resource: {
                            id: 'testDb'
                          }
                        }
                      }
                      """;
        Assert.Equal(expected, database.ToBicepSyntax().ToString());
    }

    [Fact]
    public void CreateDatabaseWithThroughputSettings()
    {
      var cosmosAccount = new CosmosDbAccountResource("test");
      var database = new CosmosDbSqlDatabaseResource(cosmosAccount, "testDb");

      // Default Databases are Serverless, Throughput settings are not allowed
      database.Throughput = 5;
      Assert.Throws<InvalidOperationException>(() => database.ToBicepSyntax());

      // Shouldn't throw
      cosmosAccount.AsStandard();
      database.ToBicepSyntax();

      // Should throw again, both set
      database.AutoscaleMaxThroughput = 5;
      Assert.Throws<InvalidOperationException>(() => database.ToBicepSyntax());

      database.AutoscaleMaxThroughput = null;

      var expected = """
                     resource testDb 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2023-11-15' = {
                       parent: cosmosDbAccount
                       name: 'testDb'
                       location: location
                       properties: {
                         options: {
                           throughput: 5
                         }
                         resource: {
                           id: 'testDb'
                         }
                       }
                     }
                     """;
      
      Assert.Equal(expected, database.ToBicepSyntax().ToString());

      database.AutoscaleMaxThroughput = 5;
      database.Throughput = null;
      
      expected = """
                 resource testDb 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2023-11-15' = {
                   parent: cosmosDbAccount
                   name: 'testDb'
                   location: location
                   properties: {
                     options: {
                       autoscaleSettings: {
                         maxThroughput: 5
                       }
                     }
                     resource: {
                       id: 'testDb'
                     }
                   }
                 }
                 """;
      
      Assert.Equal(expected, database.ToBicepSyntax().ToString());
    }
}