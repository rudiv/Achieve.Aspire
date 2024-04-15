using Achieve.Aspire.AzureProvisioning.Bicep.Internal;

namespace Achieve.Aspire.AzureProvisioning.Bicep.CosmosDb;

public class CosmosDbAccountResource : BicepResource
{
    private const string ResourceType = "Microsoft.DocumentDB/databaseAccounts@2023-11-15";
    private const string capabilityServerless = "EnableServerless";
    private const string PropertyBackupPolicy = "backupPolicy";
    private const string PropertyCapabilities = "capabilities";
    private const string PropertyConsistencyPolicy = "consistencyPolicy";
    
    public CosmosDbAccountResource(string name) : base(ResourceType)
    {
        Name = name;
    }
    
    /// <summary>
    /// The default consistency level and configuration settings of the Cosmos DB account.
    /// </summary>
    public CosmosDbConsistencyLevel ConsistencyLevel { get; set; } = CosmosDbConsistencyLevel.Session;
    /// <summary>
    /// When used with the Bounded Staleness consistency level, this value represents the number of stale requests tolerated. Accepted range for this value is 1 – 2,147,483,647. Required when defaultConsistencyPolicy is set to 'BoundedStaleness'.
    /// </summary>
    public int? MaxStalessPrefix { get; set; }
    /// <summary>
    /// When used with the Bounded Staleness consistency level, this value represents the time amount of staleness (in seconds) tolerated. Accepted range for this value is 5 - 86400. Required when defaultConsistencyPolicy is set to 'BoundedStaleness'.
    /// </summary>
    public int? MaxStalenessIntervalInSeconds { get; set; }

    /// <summary>
    /// Disable write operations on metadata resources (databases, containers, throughput) via account keys
    /// </summary>
    public bool DisableKeyBasedMetadatabaseWriteAccess { get; set; } = true;
    
    public CosmosDbNetworkAclBypass NetworkAclBypass { get; set; } = CosmosDbNetworkAclBypass.None;

    public List<BicepValue> NetworkAclBypassResourceIds { get; set; } = [];
    
    public CosmosDbPublicNetworkAccess PublicNetworkAccess { get; set; } = CosmosDbPublicNetworkAccess.SecuredByPerimiter;

    public List<CosmosDbVirtualNetworkRule> VirtualNetworkRules { get; set; } = [];
    public List<BicepValue> IpRules { get; set; } = [];
    
    /// <summary>
    /// The total throughput limit imposed on the account. A totalThroughputLimit of 2000 imposes a strict limit of max throughput that can be provisioned on that account to be 2000. Setting to null indicates no limits on provisioning of throughput.
    /// </summary>
    public int? TotalThroughputLimit { get; set; }
    
    public CosmosDbBackupPolicy BackupPolicy { get; set; } = CosmosDbBackupPolicy.Continuous;
    
    public CosmosDbContinuousBackupTier ContinuousBackupTier { get; set; } = CosmosDbContinuousBackupTier.Continuous7Days;
    
    public int? PeriodicBackupIntervalInMinutes { get; set; }
    public int? PeriodicBackupRetentionIntervalInMinutes { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public List<CosmosDbAccountLocation> Locations { get; set; } =
    [
        new CosmosDbAccountLocation
        {
            LocationName = new BicepVariableValue("location"),
            FailoverPriority = 0,
            IsZoneRedundant = false
        }
    ];

    public bool DisableLocalAuth { get; set; } = false;
    public bool EnableAnalyticalStorage { get; set; } = false;
    public bool EnableAutomaticFailover { get; set; } = false;
    public bool EnableBurstCapacity { get; set; } = false;
    public bool EnableFreeTier { get; set; } = false;

    private HashSet<string> Capabilities { get; set; } = [capabilityServerless];

    /// <summary>
    /// Run the Account as provisioned, not Serverless.
    /// </summary>
    /// <returns><see cref="CosmosDbAccountResource"/></returns>
    public CosmosDbAccountResource AsStandard()
    {
        Capabilities.Remove(capabilityServerless);
        return this;
    }

    /// <summary>
    /// Re-configures sensible defaults for development purposes.
    /// </summary>
    public CosmosDbAccountResource WithDevelopmentDefaults()
    {
        NetworkAclBypass = CosmosDbNetworkAclBypass.AzureServices;
        PublicNetworkAccess = CosmosDbPublicNetworkAccess.Enabled;
        BackupPolicy = CosmosDbBackupPolicy.Continuous;
        ContinuousBackupTier = CosmosDbContinuousBackupTier.Continuous7Days;
        DisableLocalAuth = false;

        return this;
    }

    protected override void ValidateResourceType()
    {
        if (ConsistencyLevel == CosmosDbConsistencyLevel.BoundedStaleness && (MaxStalessPrefix == null || MaxStalenessIntervalInSeconds == null))
        {
            throw new InvalidOperationException("MaxStalessPrefix and MaxStalenessIntervalInSeconds must be set when ConsistencyLevel is BoundedStaleness");
        }

        if (EnableAutomaticFailover && Locations.Count == 1)
        {
            throw new InvalidOperationException("Automatic Failover should only be enabled when there are multiple locations.");
        }

        if (EnableFreeTier && Capabilities.Contains(capabilityServerless))
        {
            throw new InvalidOperationException("Cannot enable Free Tier and Serverless at the same time. Use .AsStandard() to remove the Serverless option.");
        }

        if (Capabilities.Contains(capabilityServerless) && Locations.Count > 1)
        {
            throw new InvalidOperationException("Serverless accounts can only have one location, it can however be Zone Redundant (Locations[0].IsZoneRedundant = true).");
        }
        
        if (Capabilities.Contains(capabilityServerless) && EnableBurstCapacity)
        {
            throw new InvalidOperationException("EnableBurstCapacity cannot be used with Serverless accounts.");
        }
        
        if (NetworkAclBypass == CosmosDbNetworkAclBypass.None && NetworkAclBypassResourceIds.Count > 0)
        {
            throw new InvalidOperationException("NetworkAclBypassResourceIds can only be set when NetworkAclBypass is set to AzureServices.");
        }
        
        if (NetworkAclBypass == CosmosDbNetworkAclBypass.AzureServices && NetworkAclBypassResourceIds.Count == 0)
        {
            Console.WriteLine("Achieve CosmosDB Warning: NetworkAclBypass is set to AzureServices but no NetworkAclBypassResourceIds are set. This will allow all Azure services to access the account.");
        }

        if (BackupPolicy == CosmosDbBackupPolicy.Periodic)
        {
            if (PeriodicBackupIntervalInMinutes == null || PeriodicBackupRetentionIntervalInMinutes == null)
            {
                throw new InvalidOperationException("PeriodicBackupIntervalInMinutes and PeriodicBackupRetentionIntervalInMinutes must be set when BackupPolicy is Periodic.");
            }

            if (PeriodicBackupIntervalInMinutes < 0 || PeriodicBackupRetentionIntervalInMinutes < 0)
            {
                throw new InvalidOperationException("PeriodicBackupIntervalInMinutes and PeriodicBackupRetentionIntervalInMinutes must not be zero.");
            }
        }

        if (!DisableLocalAuth)
        {
            Console.WriteLine("Achieve CosmosDB Warning: You should disable local auth outside of development. .DisableLocalAuth = true");
        }
    }

    public override void Construct()
    {
        Body.Add(new BicepResourceProperty(BicepResourceProperties.Name,
            new BicepInterpolatedString()
                .Str(Name.ToLowerInvariant())
                .Exp(new BicepFunctionCallValue("uniqueString", new BicepPropertyAccessValue(new BicepFunctionCallValue("resourceGroup"), "id")))));
        Body.Add(new BicepResourceProperty(BicepResourceProperties.Location, new BicepVariableValue("location")));
        
        var propertyBag = new BicepResourcePropertyBag(BicepResourceProperties.Properties, 1);
        // Not needed
        //propertyBag.AddProperty(new BicepResourceProperty("kind", new BicepStringValue("GlobalDocumentDB")));
        
        AddBackupPolicy(propertyBag);
        AddCapabilities(propertyBag);
        if (DisableLocalAuth) propertyBag.AddProperty("disableLocalAuth", new BicepBooleanValue(DisableLocalAuth));
        if (EnableAutomaticFailover) propertyBag.AddProperty("enableAutomaticFailover", new BicepBooleanValue(EnableAutomaticFailover));
        if (EnableAnalyticalStorage) propertyBag.AddProperty("enableAnalyticalStorage", new BicepBooleanValue(EnableAnalyticalStorage));
        if (EnableBurstCapacity) propertyBag.AddProperty("enableBurstCapacity", new BicepBooleanValue(EnableBurstCapacity));
        
        
        Body.Add(propertyBag);

    }

    private void AddBackupPolicy(BicepResourcePropertyBag bag)
    {
        var backupPolicyBag = new BicepResourcePropertyBag(PropertyBackupPolicy, 2);
        backupPolicyBag.AddProperty(new BicepResourceProperty("type", new BicepStringValue(BackupPolicy.ToString())));
        if (BackupPolicy == CosmosDbBackupPolicy.Periodic)
        {
            var periodicBag = new BicepResourcePropertyBag("periodicModeProperties", 3);
            periodicBag.AddProperty(new BicepResourceProperty("periodicIntervalInMinutes", new BicepIntValue(PeriodicBackupIntervalInMinutes!.Value)));
            periodicBag.AddProperty(new BicepResourceProperty("periodicRetentionIntervalInMinutes", new BicepIntValue(PeriodicBackupRetentionIntervalInMinutes!.Value)));
            backupPolicyBag.AddProperty(periodicBag);
        }
        else
        {
            var continuousBag = new BicepResourcePropertyBag("continuousModeProperties", 3);
            continuousBag.AddProperty(new BicepResourceProperty("tier", new BicepStringValue(ContinuousBackupTier.ToString())));
            backupPolicyBag.AddProperty(continuousBag);
        }
        bag.AddProperty(backupPolicyBag);
    }

    private void AddCapabilities(BicepResourcePropertyBag bag)
    {
        if (Capabilities.Count == 0)
        {
            return;
        }
        var capabilityArray = new BicepResourcePropertyArray(PropertyCapabilities, 2);
        foreach(var capability in Capabilities)
        {
            capabilityArray.AddValue(new BicepStringValue(capability));
        }

        bag.AddProperty(capabilityArray);
    }
}

public class CosmosDbAccountLocation
{
    /// <summary>
    /// The name of the region.
    /// </summary>
    public BicepValue LocationName { get; set; }
    
    /// <summary>
    /// Flag to indicate whether this region is an AvailabilityZone region.
    /// </summary>
    public bool IsZoneRedundant { get; set; }
    
    /// <summary>
    /// The failover priority of the region. A failover priority of 0 indicates a write region. The maximum value for a failover priority = (total number of regions - 1). Failover priority values must be unique for each of the regions in which the database account exists.
    /// </summary>
    public int FailoverPriority { get; set; }
}

public class CosmosDbVirtualNetworkRule
{
    /// <summary>
    /// Resource ID of the Subnet.
    /// </summary>
    public BicepValue Id { get; set; }
    
    /// <summary>
    /// Create firewall rule before the virtual network has vnet service endpoint enabled.
    /// </summary>
    public bool IgnoreMissingVNetServiceEndpoint { get; set; }
}

public enum CosmosDbNetworkAclBypass
{
    None,
    AzureServices
}

public enum CosmosDbPublicNetworkAccess
{
    Disabled,
    Enabled,
    SecuredByPerimiter
}

public enum CosmosDbConsistencyLevel
{
    Eventual,
    ConsistentPrefix,
    Session,
    BoundedStaleness,
    Strong
}

public enum CosmosDbBackupPolicy
{
    Continuous,
    Periodic
}

public enum CosmosDbContinuousBackupTier
{
    Continuous7Days,
    Continuous30Days
}

public enum CosmosDbPeriodicBackupStorageRedundancy
{
    Geo,
    Local,
    Zone
}