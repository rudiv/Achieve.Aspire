using Achieve.Aspire.AzureProvisioning.Bicep.Internal;

namespace Achieve.Aspire.AzureProvisioning.Bicep.KeyVault;

public class KeyVaultResource : BicepResource
{
    private const string ResourceType = "Microsoft.KeyVault/vaults@2022-07-01";
    
    public BicepValue PublicName { get; set; }

    public KeyVaultResource() : base(ResourceType)
    {
        Name = "kv_" + Helpers.StableIdentifier("kv");
    }
    
    protected override void ValidateResourceType()
    {
        if (!Existing)
        {
            throw new InvalidOperationException("KeyVault does not support creation. Use Azure.Provisioning.");
        }
    }

    public KeyVaultResource AsExisting(BicepValue name)
    {
        Existing = true;
        PublicName = name;

        return this;
    }

    public override void Construct()
    {
        // We only support existing for now
        Body.Add(new BicepResourceProperty(BicepResourceProperties.Name, PublicName));
    }
}