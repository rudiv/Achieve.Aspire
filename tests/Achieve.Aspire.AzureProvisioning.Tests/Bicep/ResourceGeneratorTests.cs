using Achieve.Aspire.AzureProvisioning.Bicep.KeyVault;
using Achieve.Aspire.AzureProvisioning.ManagedIdentity;
using Xunit.Abstractions;

namespace Achieve.Aspire.AzureProvisioning.Tests.Bicep;

public class ResourceGeneratorTests(ITestOutputHelper output)
{
    [Fact]
    public void UserAssignedIdentityResourceGeneratesCorrectSyntax()
    {
        var userAssignedIdentity = new UserAssignedIdentityResource("test");
        var expected = """
                       resource uai_A94A8FE5 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
                         name: toLower(take('uai_A94A8FE5${uniqueString(resourceGroup().id)}'),24)
                         location: location
                       }
                       """;
        Assert.Equal(expected, userAssignedIdentity.ToBicepSyntax().ToString());
    }

    [Fact]
    public void ExistingKeyVaultIsValid()
    {
        var keyVault = new KeyVaultResource()
        {
            Existing = true
        };
    }
}