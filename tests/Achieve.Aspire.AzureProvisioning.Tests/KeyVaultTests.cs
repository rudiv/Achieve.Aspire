using System.Text.Json;
using Achieve.Aspire.AzureProvisioning.Tests.Utils;
using Aspire.Hosting;
using Azure.ResourceManager.Models;
using Bicep.Core.Diagnostics;
using Bicep.Core.Parsing;
using Bicep.Core.Syntax;
using Xunit.Abstractions;

namespace Achieve.Aspire.AzureProvisioning.Tests;

public class KeyVaultTests(ITestOutputHelper output)
{
    [Fact]
    public async Task ZeroTrustKeyVaultCanHaveManagedIdentityAndSecrets()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var id = builder.AddManagedIdentity("testid");
        var bicepOutput = builder.AddBicepTemplateString("x", "x")
            .GetOutput("test");
        var kv = builder.AddZtAzureKeyVault("kv", o =>
        {
            o.AddManagedIdentity(id, KeyVaultRoles.SecretsUser);
            o.AddSecret("test", bicepOutput);
        });
        
        Assert.Equal("kv", kv.Resource.Name);
        Assert.Equal("{kv.outputs.vaultUri}", kv.Resource.VaultUri.ValueExpression);
        
        var keyVaultManifestBicep = await ManifestUtils.GetManifestWithBicep(kv.Resource);
        var expectedManifest = """
                               {
                                 "type": "azure.bicep.v0",
                                 "connectionString": "{kv.outputs.vaultUri}",
                                 "path": "kv.module.bicep",
                                 "params": {
                                   "testid": "{testid.outputs.principalId}",
                                   "x": "{x.outputs.test}"
                                 }
                               }
                               """;
        Assert.Equal(expectedManifest, keyVaultManifestBicep.ManifestNode.ToString());
        
        // Parsing this with Bicep to account for any potential changes in any other properties that are output
        var parser = new Parser(keyVaultManifestBicep.BicepText);
        var program = parser.Program();
        Assert.Equal(9, program.Declarations.Count());

        var kvResource = program.Declarations.ElementAt(4) as ResourceDeclarationSyntax;
        var roleResource = program.Declarations.ElementAt(5) as ResourceDeclarationSyntax;
        var kvsResource = program.Declarations.ElementAt(6) as ResourceDeclarationSyntax;
        Assert.NotNull(kvResource);
        Assert.NotNull(roleResource);
        Assert.NotNull(kvsResource);

        var kvProperties = kvResource.GetBody().Properties.First(p => p.TryGetKeyText() == "properties").Value as ObjectSyntax;
        // Test for RBAC Authorization
        var enableRbac = kvProperties!.Properties.First(p => p.TryGetKeyText() == "enableRbacAuthorization").Value as BooleanLiteralSyntax;
        Assert.NotNull(enableRbac);
        Assert.True(enableRbac.Value);
        
        var roleProperties = roleResource.GetBody().Properties.First(p => p.TryGetKeyText() == "properties").Value as ObjectSyntax;
        // Correct principal parameter name
        var principalId = roleProperties!.Properties.First(p => p.TryGetKeyText() == "principalId").Value as VariableAccessSyntax;
        Assert.NotNull(principalId);
        Assert.Equal("testid", principalId.Name.ToString());
        // Correct Role Definition ID (ie. not Administrator)
        var roleDefinitionId = roleProperties!.Properties.First(p => p.TryGetKeyText() == "roleDefinitionId").Value as FunctionCallSyntax;
        Assert.NotNull(roleDefinitionId);
        Assert.Equal("4633458b-17de-408a-b874-0445c86b69e6", (roleDefinitionId.Arguments[1].Expression as StringSyntax)!.SegmentValues[0]);
        
        // Correct secret name
        var kvsName = kvsResource.GetBody().Properties.First(p => p.TryGetKeyText() == "name").Value as StringSyntax;
        Assert.NotNull(kvsName);
        Assert.Equal("test", kvsName.SegmentValues[0]);
        // Correctly referenced secret value
        var kvsProperties = kvsResource.GetBody().Properties.First(p => p.TryGetKeyText() == "properties").Value as ObjectSyntax;
        var secretValue = kvsProperties!.Properties.First(p => p.TryGetKeyText() == "value").Value as VariableAccessSyntax;
        Assert.NotNull(secretValue);
        Assert.Equal("x", secretValue.Name.ToString());
    }
}