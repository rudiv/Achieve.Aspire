using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning.Authorization;
using Azure.Provisioning.KeyVaults;
using Azure.ResourceManager.Authorization.Models;

namespace Achieve.Aspire.AzureProvisioning;

public static class KeyVaultExtensions
{
    public static IResourceBuilder<AzureKeyVaultResource> AddZtAzureKeyVault(this IDistributedApplicationBuilder builder, string name, Action<KeyVaultOptions> kvBuilder)
        => AddZtAzureKeyVault(builder, name, kvBuilder, null);
    
    public static IResourceBuilder<AzureKeyVaultResource> AddZtAzureKeyVault(this IDistributedApplicationBuilder builder, string name, Action<KeyVaultOptions> kvBuilder, Action<IResourceBuilder<AzureKeyVaultResource>, ResourceModuleConstruct, KeyVault>? configureResource)
    {
        builder.AddAzureProvisioning();

        var kvOpts = new KeyVaultOptions();
        kvBuilder(kvOpts);

        var configureConstruct = (ResourceModuleConstruct construct) =>
        {
            var keyVault = construct.AddKeyVault(name: construct.Resource.Name);
            keyVault.AddOutput("vaultUri", x => x.Properties.VaultUri);

            keyVault.Properties.Tags["aspire-resource-name"] = construct.Resource.Name;

            // ARGH Azure.Provisioning still contains too much magic 🤬
            // If you're reading this, the RA's "name" is a calculated GUID but it uses the principalId parameter which we don't want.
            for (int i = 0; i < kvOpts.ManagedIdentities.Count; i++)
            {
                var (identity, role) = kvOpts.ManagedIdentities[i];
                var raGuid = Helpers.StableGuid(identity.Resource.Name);
                var roleAssignment = keyVault.AssignRole(
                    role,
                    principalId: raGuid,
                    principalType: RoleManagementPrincipalType.ServicePrincipal);
                roleAssignment.AssignProperty(x => x.PrincipalId, identity.Resource.PrincipalId);
            }

            foreach (var (name, opr) in kvOpts.Secrets)
            {
                var kvSecret = new KeyVaultSecret(construct, keyVault, name);
                kvSecret.AssignProperty(x => x.Properties.Value, opr);
            }

            var resource = (AzureKeyVaultResource)construct.Resource;
            var resourceBuilder = builder.CreateResourceBuilder(resource);
            configureResource?.Invoke(resourceBuilder, construct, keyVault);
        };
        var resource = new AzureKeyVaultResource(name, configureConstruct);

        return builder.AddResource(resource)
            .WithManifestPublishingCallback(resource.WriteToManifest);
    }
}

public static class KeyVaultRoles
{
    public static RoleDefinition Reader { get; } = new("21090545-7ca7-4776-b22c-e363652d74d2");
    public static RoleDefinition CertificateUser { get; } = new("db79e9a7-68ee-4b58-9aeb-b90e7c24fcba");
    public static RoleDefinition SecretsUser { get; } = new("4633458b-17de-408a-b874-0445c86b69e6");
}

public class KeyVaultOptions
{
    public List<(IResourceBuilder<AzureManagedIdentityResource>, RoleDefinition)> ManagedIdentities { get; set; } = [];
    public List<(string, BicepOutputReference)> Secrets { get; set; } = [];
    
    /// <summary>
    /// Add a Managed Identity to this Key Vault with the specified role.
    /// </summary>
    /// <param name="identity"></param>
    /// <param name="role"></param>
    public void AddManagedIdentity(IResourceBuilder<AzureManagedIdentityResource> identity, RoleDefinition role)
    {
        ManagedIdentities.Add((identity, role));
    }

    /// <summary>
    /// Add a Secret to the Key Vault based on Bicep output.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    public void AddSecret(string name, BicepOutputReference value)
    {
        Secrets.Add((name, value));
    }
}