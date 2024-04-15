using Achieve.Aspire.AzureProvisioning.Bicep.CosmosDb;
using Xunit.Abstractions;

namespace Achieve.Aspire.AzureProvisioning.Tests.Bicep.Cosmos;

public class AccountTests(ITestOutputHelper output)
{
    [Fact]
    public void CreateDefaultAccount()
    {
        var cosmosAccount = new CosmosDbAccountResource("test");
        
        output.WriteLine(cosmosAccount.ToBicepSyntax().ToString());
    }
}