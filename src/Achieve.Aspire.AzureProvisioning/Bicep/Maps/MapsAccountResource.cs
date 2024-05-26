using Achieve.Aspire.AzureProvisioning.Bicep.Internal;

namespace Achieve.Aspire.AzureProvisioning.Bicep.Maps;

public sealed class MapsAccountResource : BicepResource
{
    private const string resourceType = "Microsoft.Maps/accounts@2023-06-01";

    public MapsAccountResource(string name) : base(resourceType)
    {
        AccountName = name;
        Name = "mapsAccount";
    }

    public string AccountName { get; set; }

    public string Sku { get; set; } = "G2";
    public string Kind { get; set; } = "Gen2";

    public bool DisableLocalAuth { get; set; } = true;

    public override BicepResource AsExisting() => new ExistingBicepResource(resourceType, AccountName);

    public override void Construct()
    {
        Body.Add(new BicepResourceProperty(BicepResourceProperties.Name,
            new BicepInterpolatedString()
                .Str(AccountName.ToLowerInvariant())
                .Exp(new BicepFunctionCallValue("uniqueString", new BicepPropertyAccessValue(new BicepFunctionCallValue("resourceGroup"), "id")))));
        Body.Add(new BicepResourceProperty(BicepResourceProperties.Location, new BicepVariableValue("location")));
        Body.Add(new BicepResourceProperty("kind", new BicepStringValue(Kind)));
        var skuBag = new BicepResourcePropertyBag("sku", 1);
        skuBag.AddProperty("name", new BicepStringValue(Sku));
        Body.Add(skuBag);

        var propertyBag = new BicepResourcePropertyBag(BicepResourceProperties.Properties, 1);
        if (DisableLocalAuth)
        {
            propertyBag.AddProperty("disableLocalAuth", new BicepBooleanValue(true));
        }
        Body.Add(propertyBag);
    }
}