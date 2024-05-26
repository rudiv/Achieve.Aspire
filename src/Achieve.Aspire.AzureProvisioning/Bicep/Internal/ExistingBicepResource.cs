namespace Achieve.Aspire.AzureProvisioning.Bicep.Internal;

public class ExistingBicepResource : BicepResource
{
    private readonly string resourceName;
    
    public ExistingBicepResource(string type, string name) : base(type)
    {
        resourceName = name;
        Existing = true;
        Name = "er_" + Helpers.StableIdentifier(name);
    }
    
    public override void Construct()
    {
        Body.Add(new BicepResourceProperty("name", new BicepStringValue(resourceName)));
    }
}