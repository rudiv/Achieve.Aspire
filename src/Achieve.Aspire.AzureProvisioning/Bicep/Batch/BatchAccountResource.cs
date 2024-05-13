using System.Runtime.Serialization;
using Achieve.Aspire.AzureProvisioning.Bicep.Internal;
using Achieve.Aspire.AzureProvisioning.Extensions;

namespace Achieve.Aspire.AzureProvisioning.Bicep.Batch;

public sealed class BatchAccountResource : BicepResource
{
    private const string resourceType = "Microsoft.Batch/batchAccounts@2023-11-01";
    private const string propertyAllowedAuthenticationModes = "allowedAuthenticationModes";
    private const string propertyAutoStorage = "autoStorage";
    private const string propertyEncryption = "encryption";
    private const string propertyKeyVaultReference = "keyVaultReference";
    private const string propertyNetworkProfile = "networkProfile";
    private const string propertyPoolAllocationMode = "poolAllocationMode";
    private const string propertyPublicNetworkAccess = "publicNetworkAccess";
    
    public BatchAccountResource(string name) : base(resourceType)
    {
        AccountName = name;
        Name = "batchAccount";
    }
    
    /// <summary>
    /// The name of the account.
    /// </summary>
    /// <remarks>
    /// 3-24 characters, accepts lowercase letters and numbers.
    /// </remarks>
    public string AccountName { get; set; }
    
    /// <summary>
    /// The user-specified tags associated with the account.
    /// </summary>
    public Dictionary<string, string> Tags { get; set; } = [];
    
    /// <summary>
    /// The identity of the Batch account.
    /// </summary>
    public BatchAccountIdentity Identity { get; set; }
    
    /// <summary>
    /// The properties of the account.
    /// </summary>
    public BatchAccountCreatePropertiesOrBatchAccountProperties Properties { get; set; }

    protected override void ValidateResourceType()
    {
        if (Name.MatchesConstraints(3, 24, StringExtensions.CharacterClass.LowercaseLetter))
        {
            throw new InvalidOperationException(
                "Name must be between 3-24 characters and must contain only lowercase letters and numbers");
        }

        if (Identity is {Type: BatchAccountIdentityType.UserAssigned, UserAssignedIdentityResourceIds.Count: 0})
        {
            throw new InvalidOperationException(
                "If the resource uses User-Assigned Managed Identities, they must be assigned to the resource");
        }

        if (Properties.Encryption.KeySource == EncryptionKeySource.KeyVault &&
            string.IsNullOrWhiteSpace(Properties.Encryption.KeyVaultProperties?.KeyIdentifier))
        {
            throw new InvalidOperationException(
                "If a Key Vault is being used as the encryption key source, it must be specified");
        }

        if (Properties is {PublicNetworkAccess: PublicNetworkAccess.Enabled, NetworkProfile: null})
        {
            throw new InvalidOperationException(
                "If public network access is enabled, the network profile must be provided");
        }
    }
    
    public override void Construct()
    {
        Body.Add(new BicepResourceProperty(BicepResourceProperties.Name,
            new BicepInterpolatedString()
                .Str(AccountName.ToLowerInvariant())
                .Exp(new BicepFunctionCallValue("uniqueString",
                    new BicepPropertyAccessValue(new BicepFunctionCallValue("resourceGroup"), "id")))));
        Body.Add(new BicepResourceProperty("location", new BicepVariableValue("location")));

        var propertyBag = new BicepResourcePropertyBag(BicepResourceProperties.Properties, 1);
        AddAllowedAuthenticationModes(propertyBag);
        AddAutoStorage(propertyBag);
        AddEncryption(propertyBag);
        AddKeyVaultReference(propertyBag);
        if (Properties.PublicNetworkAccess == PublicNetworkAccess.Enabled) AddNetworkProfile(propertyBag);
        propertyBag.AddProperty(propertyPoolAllocationMode,
            new BicepStringValue(Properties.PoolAllocationMode.GetValueFromEnumMember()));
        propertyBag.AddProperty(propertyPublicNetworkAccess,
            new BicepStringValue(Properties.PublicNetworkAccess.GetValueFromEnumMember()));

        Body.Add(propertyBag);
    }

    private void AddAllowedAuthenticationModes(BicepResourcePropertyBag bag)
    {
        var authModes = new BicepResourcePropertyArray(propertyAllowedAuthenticationModes, 2);
        foreach (var authMode in Properties.AllowedAuthenticationModes)
        {
            authModes.AddValue(new BicepStringValue(authMode.GetValueFromEnumMember()));
        }
        bag.AddProperty(authModes);
    }

    private void AddAutoStorage(BicepResourcePropertyBag bag)
    {
        var autoStorageBag = new BicepResourcePropertyBag(propertyAutoStorage, 2);
        autoStorageBag.AddProperty("authenticationMode",
            new BicepStringValue(Properties.AutoStorage.AuthenticationMode.GetValueFromEnumMember()));

        var identityReferenceBag = new BicepResourcePropertyBag("nodeIdentityReference", 3)
            .AddProperty("resourceId", new BicepStringValue(Properties.AutoStorage.NodeIdentityReference.ResourceId));
        autoStorageBag.AddProperty(identityReferenceBag);

        autoStorageBag.AddProperty("storageAccountId", new BicepStringValue(Properties.AutoStorage.StorageAccountId));
    }

    private void AddEncryption(BicepResourcePropertyBag bag)
    {
        var encryptionBag = new BicepResourcePropertyBag(propertyEncryption, 2);
        encryptionBag.AddProperty("keySource",
            new BicepStringValue(Properties.Encryption.KeySource.GetValueFromEnumMember()));
        if (Properties.Encryption is {KeySource: EncryptionKeySource.KeyVault, KeyVaultProperties: not null})
        {
            var keyVaultPropertiesBag = new BicepResourcePropertyBag("keyVaultProperties", 3);
            keyVaultPropertiesBag.AddProperty("keyIdentifier",
                new BicepStringValue(Properties.Encryption.KeyVaultProperties.KeyIdentifier));
        }
    }

    private void AddKeyVaultReference(BicepResourcePropertyBag bag)
    {
        var keyVaultBag = new BicepResourcePropertyBag(propertyKeyVaultReference, 2);
        keyVaultBag.AddProperty("id", new BicepStringValue(Properties.KeyVaultReference.Id));
        keyVaultBag.AddProperty("url", new BicepStringValue(Properties.KeyVaultReference.Url));
    }

    private void AddNetworkProfile(BicepResourcePropertyBag bag)
    {
        var networkProfileBag = new BicepResourcePropertyBag(propertyNetworkProfile, 2);
        if (Properties.NetworkProfile != null)
        {
            //Account access
            var accountAccessBag = new BicepResourcePropertyBag("accountAccess", 3)
                .AddProperty("defaultAccess",
                    new BicepStringValue(Properties.NetworkProfile.AccountAccess.DefaultAction
                        .GetValueFromEnumMember()));
            
            var accountIpRules = new BicepResourcePropertyArray("ipRules", 4);
            foreach (var rule in Properties.NetworkProfile.AccountAccess.IpRules)
            {
                var rulePropertyBag = new BicepResourcePropertyBag("ip");
                rulePropertyBag.AddProperty("action", new BicepStringValue(rule.Action));
                rulePropertyBag.AddProperty("value", new BicepStringValue(rule.Value));
                accountIpRules.AddValue(rulePropertyBag);
            }
            networkProfileBag.AddProperty(accountAccessBag);
            
            
            //NOde management access
            var nodeManagementAccessBag = new BicepResourcePropertyBag("nodeManagementAccess", 3)
                .AddProperty("defaultAccess",
                    new BicepStringValue(
                        Properties.NetworkProfile.NodeManagementAccess.DefaultAction.GetValueFromEnumMember()));
            
            var nodeIpRules = new BicepResourcePropertyArray("ipRules", 4);
            foreach (var rule in Properties.NetworkProfile.NodeManagementAccess.IpRules)
            {
                var rulePropertyBag = new BicepResourcePropertyBag("ip");
                rulePropertyBag.AddProperty("action", new BicepStringValue(rule.Action));
                rulePropertyBag.AddProperty("value", new BicepStringValue(rule.Value));
                nodeIpRules.AddValue(rulePropertyBag);
            }
            networkProfileBag.AddProperty(accountAccessBag);
        }
    }
}

public class BatchAccountIdentity
{
    /// <summary>
    /// The type of identity used for the Batch account.
    /// </summary>
    public BatchAccountIdentityType Type { get; set; }

    public List<string> UserAssignedIdentityResourceIds { internal get; set; } = [];

    /// <summary>
    /// The list of user identities associated with the Batch account.
    /// </summary>
    public Dictionary<string, string> UserAssignedIdentities =>
        UserAssignedIdentityResourceIds.ToDictionary(resourceId => resourceId, resourceId => "");
}

public class BatchAccountCreatePropertiesOrBatchAccountProperties
{
    /// <summary>
    /// List of allowed authentication modes for the Batch account that can be used to authenticate
    /// with the data plane. This does not affect authentication with the control plane.
    /// </summary>
    public List<AllowedAuthenticationMode> AllowedAuthenticationModes { get; set; } = [];
    /// <summary>
    /// The properties related to the auto-storage account.
    /// </summary>
    public AutoStorageBasePropertiesOrAutoStorageProperties AutoStorage { get; set; }
    /// <summary>
    /// Configures how customer data is encrypted inside the Batch account. By default, accounts
    /// are encrypted using a Microsoft managed key.
    /// </summary>
    public EncryptionProperties Encryption { get; set; }
    /// <summary>
    /// A reference to the Azure key vault associated with the Batch account.
    /// </summary>
    public KeyVaultReference KeyVaultReference { get; set; }
    /// <summary>
    /// The network profile only takes effect when publicNetworkAccess is enabled.
    /// </summary>
    public NetworkProfile? NetworkProfile { get; set; }
    /// <summary>
    /// The pool allocation mode also affects how clients may authenticate to the Batch Service
    /// API. If the mode is BatchService, clients  may authenticate using access keys or
    /// Microsoft Entra ID. If the mode is UserSubscription, clients must use Microsoft Entra ID.
    /// The default is BatchService.
    /// </summary>
    public PoolAllocationMode PoolAllocationMode { get; set; } = PoolAllocationMode.BatchService;
    /// <summary>
    /// The default is enabled.
    /// </summary>
    public PublicNetworkAccess PublicNetworkAccess { get; set; } = PublicNetworkAccess.Enabled;
}

public class AutoStorageBasePropertiesOrAutoStorageProperties
{
    /// <summary>
    /// The authentication mode which the Batch service will use to manage the auto-storage
    /// account.
    /// </summary>
    public AutoStorageAuthenticationMode AuthenticationMode { get; set; }
    
    /// <summary>
    /// The identity referenced here must be assigned to pools which have compute
    /// nodes that need access to auto-storage.
    /// </summary>
    public ComputeNodeIdentityReference NodeIdentityReference { get; set; }
    
    /// <summary>
    /// The resource ID of the storage account to be used for auto-storage account.
    /// </summary>
    public string StorageAccountId { get; set;}
}

public class ComputeNodeIdentityReference
{
    /// <summary>
    /// The ARM resource ID of the user assigned identity. 
    /// </summary>
    public string ResourceId { get; set; }
}

public class EncryptionProperties
{
    /// <summary>
    /// Type of the key source.
    /// </summary>
    public EncryptionKeySource KeySource { get; set; }
    
    /// <summary>
    /// Additional details when using a Key Vault.
    /// </summary>
    public KeyVaultProperties? KeyVaultProperties { get; set; }
}

public class KeyVaultProperties
{
    /// <summary>
    /// Full path to the secret with or without version.
    /// </summary>
    /// <example>
    /// https://mykeyvault.vault.azure.net/keys/testkey/6e34a81fef704045975661e297a4c053
    /// </example>
    /// <remarks>
    /// To be usable, the following prerequisites must be met:
    ///
    /// - The Batch account has a System Assigned identity
    /// - The account identity has been granted Key/Get, Key/Unwrap and Key/Wrap permissions
    /// - The key vault has soft-delete and purge protection enabled 
    /// </remarks>
    public string KeyIdentifier { get; set; }
}

public class KeyVaultReference
{
    /// <summary>
    /// The resource ID of the Azure key vault associated with the Batch account.
    /// </summary>
    public string Id { get; set; }
    
    /// <summary>
    /// The URL of the Azure Key vault associated with the Batch account.
    /// </summary>
    public string Url { get; set; }
}

public class NetworkProfile
{
    /// <summary>
    /// Network access profile for batchAccount endpoint (Batch account data plane API).
    /// </summary>
    public EndpointAccessProfile AccountAccess { get; set; }
    /// <summary>
    /// Network access profile for nodeManagement endpoint (Batch service managing compute
    /// nodes for Batch pools.
    /// </summary>
    public EndpointAccessProfile NodeManagementAccess { get; set; }
}

public class EndpointAccessProfile
{
    /// <summary>
    /// Default action for endpoint access. It is only applicable when publicNetworkAccess is enabled.
    /// </summary>
    public required EndpointAccessProfileDefaultAction DefaultAction { get; set; }
    /// <summary>
    /// Array of IP ranges to filter client IP address.
    /// </summary>
    public List<IpRule> IpRules { get; set; } = [];
}

public class IpRule
{
    /// <summary>
    /// Action when client IP address is matched.
    /// </summary>
    public string Action => "action";
    /// <summary>
    /// IPv4 address, or IPv4 address range in CIDR format.
    /// </summary>
    public string Value { get; set; }
}

public enum BatchAccountIdentityType
{
    None,
    SystemAssigned,
    UserAssigned
}

public enum EndpointAccessProfileDefaultAction
{
    Allow,
    Deny
}

public enum AllowedAuthenticationMode
{
    [EnumMember(Value="AAD")]
    Entra,
    [EnumMember(Value="SharedKey")]
    SharedKey,
    [EnumMember(Value="TaskAuthenticationToken")]
    TaskAuthenticationToken
}

public enum PoolAllocationMode
{
    BatchService,
    UserSubscription
}

public enum PublicNetworkAccess
{
    Enabled,
    Disabled
}

public enum AutoStorageAuthenticationMode
{
    BatchAccountManagedIdentity,
    StorageKeys
}

public enum EncryptionKeySource
{
    [EnumMember(Value="Microsoft.Batch")]
    Batch,
    [EnumMember(Value="Microsoft.KeyVault")]
    KeyVault
}