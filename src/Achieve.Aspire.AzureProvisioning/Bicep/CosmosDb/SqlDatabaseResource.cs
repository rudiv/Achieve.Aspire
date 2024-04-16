using Achieve.Aspire.AzureProvisioning.Bicep.Internal;

namespace Achieve.Aspire.AzureProvisioning.Bicep.CosmosDb;

public class CosmosDbSqlDatabaseResource : BicepResource
{
    private const string resourceType = "Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2023-11-15";
    
    public CosmosDbAccountResource Parent { get; set; }
    
    public CosmosDbSqlDatabaseResource(CosmosDbAccountResource parent, string name) : base(resourceType)
    {
        Parent = parent;
        Name = name;
    }
    
    public int? AutoscaleMaxThroughput { get; set; }
    public int? Throughput { get; set; }

    protected override void ValidateResourceType()
    {
        if (Parent.Capabilities.Contains(CosmosDbAccountResource.CapabilityServerless))
        {
            if (Throughput != null || AutoscaleMaxThroughput != null)
            {
                throw new InvalidOperationException("Cannot set throughput on Database when Account is Serverless.");
            }
        } else if (Throughput != null && AutoscaleMaxThroughput != null)
        {
            throw new InvalidOperationException("Cannot set both Throughput and AutoscaleMaxThroughput on Database.");
        }
    }

    public override void Construct()
    {
        Body.Add(new BicepResourceProperty("parent", new BicepVariableValue(Parent.Name)));
        Body.Add(new BicepResourceProperty("name", new BicepStringValue(Name)));
        Body.Add(new BicepResourceProperty("location", new BicepVariableValue("location")));
        
        var propertyBag = new BicepResourcePropertyBag(BicepResourceProperties.Properties);
        if (Throughput != null || AutoscaleMaxThroughput != null)
        {
            var optionsBag = new BicepResourcePropertyBag("options", 2);
            if (AutoscaleMaxThroughput != null)
            {
                var autoScaleSettingsBag = new BicepResourcePropertyBag("autoscaleSettings", 3);
                autoScaleSettingsBag.AddProperty("maxThroughput", new BicepIntValue(AutoscaleMaxThroughput.Value));
                optionsBag.AddProperty(autoScaleSettingsBag);
            } else if (Throughput != null)
            {
                optionsBag.AddProperty("throughput", new BicepIntValue(Throughput.Value));
            }

            propertyBag.AddProperty(optionsBag);
        }

        var resourceBag = new BicepResourcePropertyBag("resource", 2)
            .AddProperty("id", new BicepStringValue(Name));
        propertyBag.AddProperty(resourceBag);
        Body.Add(propertyBag);
    }
}