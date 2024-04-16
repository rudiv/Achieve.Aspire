using Achieve.Aspire.AzureProvisioning.Bicep.CosmosDb;
using Achieve.Aspire.AzureProvisioning.Bicep.Internal;
using Xunit.Abstractions;

namespace Achieve.Aspire.AzureProvisioning.Tests.Bicep.Cosmos;

public class RoleAssignmentTests(ITestOutputHelper output)
{
    [Fact]
    public void CreateAccountScopeRoleAssignment()
    {
        var account = new CosmosDbAccountResource("test");
        var cosmosRoleAssignment = new CosmosDbSqlRoleAssignmentResource("testIdAccess")
          .WithScope(account)
          .WithDefaultPrincipalId()
          .WithContributorRole();
        
        output.WriteLine(cosmosRoleAssignment.ToBicepSyntax().ToString());

        var expected = """
                      resource testIdAccess 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2023-11-15' = {
                        parent: cosmosDbAccount
                        name: guid(cosmosDbAccount.id,'testIdAccess')
                        properties: {
                          roleDefinitionId: resourceId('Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions',cosmosDbAccount.name,'00000000-0000-0000-0000-000000000002')
                          scope: cosmosDbAccount.id
                          principalId: principalId
                        }
                      }
                      """;
        Assert.Equal(expected, cosmosRoleAssignment.ToBicepSyntax().ToString());
    }
    
    [Fact]
    public void CreateDatabaseScopeRoleAssignment()
    {
        var account = new CosmosDbAccountResource("test");
        var database = new CosmosDbSqlDatabaseResource(account, "testDb");
        var cosmosRoleAssignment = new CosmosDbSqlRoleAssignmentResource("testIdAccess")
            .WithScope(database)
            .WithDefaultPrincipalId()
            .WithContributorRole();

        var expected = """
                       resource testIdAccess 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2023-11-15' = {
                         parent: cosmosDbAccount
                         name: guid(cosmosDbAccount.id,'testIdAccess')
                         properties: {
                           roleDefinitionId: resourceId('Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions',cosmosDbAccount.name,'00000000-0000-0000-0000-000000000002')
                           scope: '${cosmosDbAccount.id}/dbs/${testDb.name}'
                           principalId: principalId
                         }
                       }
                       """;
        Assert.Equal(expected, cosmosRoleAssignment.ToBicepSyntax().ToString());
    }
    
    [Fact]
    public void CreateCollectionScopeRoleAssignment()
    {
        var account = new CosmosDbAccountResource("test");
        var database = new CosmosDbSqlDatabaseResource(account, "testDb");
        var collection = new CosmosDbSqlContainerResource(database, "testColl");
        var cosmosRoleAssignment = new CosmosDbSqlRoleAssignmentResource("testIdAccess")
            .WithScope(collection)
            .WithDefaultPrincipalId()
            .WithContributorRole();

        var expected = """
                       resource testIdAccess 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2023-11-15' = {
                         parent: cosmosDbAccount
                         name: guid(cosmosDbAccount.id,'testIdAccess')
                         properties: {
                           roleDefinitionId: resourceId('Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions',cosmosDbAccount.name,'00000000-0000-0000-0000-000000000002')
                           scope: '${cosmosDbAccount.id}/dbs/${testDb.name}/colls/${testColl.name}'
                           principalId: principalId
                         }
                       }
                       """;
        Assert.Equal(expected, cosmosRoleAssignment.ToBicepSyntax().ToString());
    }

}