using Achieve.Aspire.AzureProvisioning.Bicep.Internal;

namespace Achieve.Aspire.AzureProvisioning.Bicep.CosmosDb;

public class CosmosDbAccountResource : BicepResource
{
    private const string resourceType = "Microsoft.DocumentDB/databaseAccounts@2023-11-15";
    private const string propertyBackupPolicy = "backupPolicy";
    private const string propertyCapabilities = "capabilities";
    private const string propertyConsistencyPolicy = "consistencyPolicy";
    private const string propertyIpRules = "ipRules";
    private const string propertyLocations = "locations";
    private const string propertyNetworkAclBypass = "networkAclBypass";
    private const string propertyNetworkAclBypassResourceIds = "networkAclBypassResourceIds";
    
    public const string CapabilityServerless = "EnableServerless";
    
    public CosmosDbAccountResource(string name) : base(resourceType)
    {
        AccountName = name;
        Name = "cosmosDbAccount";
    }
    
    public string AccountName { get; set; }
    
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
    public CosmosDbPeriodicBackupStorageRedundancy PeriodicBackupStorageRedundancy { get; set; } = CosmosDbPeriodicBackupStorageRedundancy.Geo;

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
    public bool EnableMultipleWriteLocations { get; set; } = false;
    public bool EnablePriorityBasedExecution { get; set; } = false;

    public bool IsVirtualNetworkFilterEnabled { get; set; } = false;

    public CosmosDbMinimumTlsVersion MinimumTlsVersion { get; set; } = CosmosDbMinimumTlsVersion.Tls12;

    public HashSet<string> Capabilities { get; set; } = [CapabilityServerless];

    /// <summary>
    /// Run the Account as provisioned, not Serverless.
    /// </summary>
    /// <returns><see cref="CosmosDbAccountResource"/></returns>
    public CosmosDbAccountResource AsStandard()
    {
        Capabilities.Remove(CapabilityServerless);
        return this;
    }

    /// <summary>
    /// Re-configure the resource to have defaults that are good for development purposes.
    ///
    /// Note that this should very much not be used in production as it:
    /// - Configures Serverless Mode
    /// - Allows Public Network Access
    /// - Reduces to minimum Backup Policy
    /// - Allows Azure Service access (Data Explorer)
    /// - Enables Local Authentication (Account Keys)
    /// </summary>
    public CosmosDbAccountResource WithDevelopmentDefaults()
    {
        Capabilities = [CapabilityServerless];
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

        if (Locations.Count == 0)
        {
            throw new InvalidOperationException("You need to add a location.");
        }

        if (Capabilities.Contains(CapabilityServerless))
        {
            if (EnableFreeTier)
            {
                throw new InvalidOperationException("Cannot enable Free Tier and Serverless at the same time. Use .AsStandard() to remove the Serverless option.");
            }
            if (Locations.Count > 1)
            {
                throw new InvalidOperationException("Serverless accounts can only have one location, it can however be Zone Redundant (Locations[0].IsZoneRedundant = true).");
            }
            if (EnableBurstCapacity)
            {
                throw new InvalidOperationException("EnableBurstCapacity cannot be used with Serverless accounts.");
            }
            if (EnableMultipleWriteLocations)
            {
                throw new InvalidOperationException("EnableMultipleWriteLocations cannot be used with Serverless accounts.");
            }
            if (EnablePriorityBasedExecution)
            {
                throw new InvalidOperationException("EnablePriorityBasedExecution cannot be used with Serverless accounts.");
            }
        }

        if (VirtualNetworkRules.Count > 0 && !IsVirtualNetworkFilterEnabled)
        {
            throw new InvalidOperationException("VirtualNetworkRules will not be honoured unless IsVirtualNetworkFilterEnabled to true.");
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
                .Str(AccountName.ToLowerInvariant())
                .Exp(new BicepFunctionCallValue("uniqueString", new BicepPropertyAccessValue(new BicepFunctionCallValue("resourceGroup"), "id")))));
        Body.Add(new BicepResourceProperty(BicepResourceProperties.Location, new BicepVariableValue("location")));
        
        var propertyBag = new BicepResourcePropertyBag(BicepResourceProperties.Properties, 1);
        // Not needed
        //propertyBag.AddProperty(new BicepResourceProperty("kind", new BicepStringValue("GlobalDocumentDB")));
        
        AddBackupPolicy(propertyBag);
        AddCapabilities(propertyBag);
        AddConsistencyPolicy(propertyBag);
        if (TotalThroughputLimit != null)
            propertyBag.AddProperty(new BicepResourcePropertyBag("capacity").AddProperty("totalThroughputLimit", new BicepIntValue(TotalThroughputLimit.Value)));
        if (DisableLocalAuth) propertyBag.AddProperty("disableLocalAuth", new BicepBooleanValue(DisableLocalAuth));
        if (EnableAutomaticFailover) propertyBag.AddProperty("enableAutomaticFailover", new BicepBooleanValue(EnableAutomaticFailover));
        if (EnableAnalyticalStorage) propertyBag.AddProperty("enableAnalyticalStorage", new BicepBooleanValue(EnableAnalyticalStorage));
        if (EnableBurstCapacity) propertyBag.AddProperty("enableBurstCapacity", new BicepBooleanValue(EnableBurstCapacity));
        if (EnableFreeTier) propertyBag.AddProperty("enableFreeTier", new BicepBooleanValue(EnableFreeTier));
        if (EnableMultipleWriteLocations) propertyBag.AddProperty("enableMultipleWriteLocations", new BicepBooleanValue(EnableMultipleWriteLocations));
        if (EnablePriorityBasedExecution) propertyBag.AddProperty("enablePriorityBasedExecution", new BicepBooleanValue(EnablePriorityBasedExecution));
        AddIpRules(propertyBag);
        if (IsVirtualNetworkFilterEnabled) propertyBag.AddProperty("isVirtualNetworkFilterEnabled", new BicepBooleanValue(IsVirtualNetworkFilterEnabled));
        AddLocations(propertyBag);
        propertyBag.AddProperty("minimumTlsVersion", new BicepStringValue(MinimumTlsVersion.ToString()));
        if (NetworkAclBypass != CosmosDbNetworkAclBypass.None)
        {
            propertyBag.AddProperty(propertyNetworkAclBypass, new BicepStringValue(NetworkAclBypass.ToString()));
            // Only here as a guard for future values
            if (NetworkAclBypass == CosmosDbNetworkAclBypass.AzureServices && NetworkAclBypassResourceIds.Count > 0)
            {
                var networkAclBypassResourceIdsArray = new BicepResourcePropertyArray(propertyNetworkAclBypassResourceIds, 2);
                foreach(var resourceId in NetworkAclBypassResourceIds)
                {
                    networkAclBypassResourceIdsArray.AddValue(resourceId);
                }
                propertyBag.AddProperty(networkAclBypassResourceIdsArray);
            }
        }
        propertyBag.AddProperty("publicNetworkAccess", new BicepStringValue(PublicNetworkAccess.ToString()));
        if (IsVirtualNetworkFilterEnabled && VirtualNetworkRules.Count > 0)
        {
            var virtualNetworkRulesArray = new BicepResourcePropertyArray("virtualNetworkRules", 2);
            foreach(var rule in VirtualNetworkRules)
            {
                var ruleBag = new BicepResourcePropertyBag("virtualNetworkRule", 3);
                ruleBag.AddProperty("id", rule.Id);
                if (rule.IgnoreMissingVNetServiceEndpoint) ruleBag.AddProperty("ignoreMissingVNetServiceEndpoint", new BicepBooleanValue(rule.IgnoreMissingVNetServiceEndpoint));
                virtualNetworkRulesArray.AddValue(ruleBag);
            }
        }
        
        Body.Add(propertyBag);

    }

    private void AddBackupPolicy(BicepResourcePropertyBag bag)
    {
        var backupPolicyBag = new BicepResourcePropertyBag(propertyBackupPolicy, 2);
        backupPolicyBag.AddProperty(new BicepResourceProperty("type", new BicepStringValue(BackupPolicy.ToString())));
        if (BackupPolicy == CosmosDbBackupPolicy.Periodic)
        {
            var periodicBag = new BicepResourcePropertyBag("periodicModeProperties", 3);
            periodicBag.AddProperty(new BicepResourceProperty("periodicIntervalInMinutes", new BicepIntValue(PeriodicBackupIntervalInMinutes!.Value)));
            periodicBag.AddProperty(new BicepResourceProperty("periodicRetentionIntervalInMinutes", new BicepIntValue(PeriodicBackupRetentionIntervalInMinutes!.Value)));
            periodicBag.AddProperty(new BicepResourceProperty("backupStorageRedundancy", new BicepStringValue(PeriodicBackupStorageRedundancy.ToString())));
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
        var capabilityArray = new BicepResourcePropertyArray(propertyCapabilities, 2);
        foreach(var capability in Capabilities)
        {
            capabilityArray.AddValue(new BicepStringValue(capability));
        }

        bag.AddProperty(capabilityArray);
    }
    
    private void AddConsistencyPolicy(BicepResourcePropertyBag bag)
    {
        var consistencyPolicyBag = new BicepResourcePropertyBag(propertyConsistencyPolicy, 2);
        consistencyPolicyBag.AddProperty("defaultConsistencyLevel", new BicepStringValue(ConsistencyLevel.ToString()));
        if (ConsistencyLevel == CosmosDbConsistencyLevel.BoundedStaleness)
        {
            consistencyPolicyBag.AddProperty("maxIntervalInSeconds", new BicepIntValue(MaxStalenessIntervalInSeconds!.Value));
            consistencyPolicyBag.AddProperty("maxStalenessPrefix", new BicepIntValue(MaxStalessPrefix!.Value));
        }
        bag.AddProperty(consistencyPolicyBag);
    }
    
    private void AddIpRules(BicepResourcePropertyBag bag)
    {
        if (IpRules.Count == 0)
        {
            return;
        }
        
        var ipRulesArray = new BicepResourcePropertyArray(propertyIpRules, 2);
        foreach(var ipRule in IpRules)
        {
            ipRulesArray.AddValue(new BicepResourcePropertyBag("ip").AsValueOnly().AddProperty("ipAddressOrRange", ipRule));
        }

        bag.AddProperty(ipRulesArray);
    }
    
    private void AddLocations(BicepResourcePropertyBag bag)
    {
        var locationsArray = new BicepResourcePropertyArray(propertyLocations, 2);
        foreach(var location in Locations)
        {
            var locationBag = new BicepResourcePropertyBag("location", 3).AsValueOnly();
            locationBag.AddProperty("failoverPriority", new BicepIntValue(location.FailoverPriority));
            locationBag.AddProperty("locationName", location.LocationName);
            locationBag.AddProperty("isZoneRedundant", new BicepBooleanValue(location.IsZoneRedundant));
            locationsArray.AddValue(locationBag);
        }

        bag.AddProperty(locationsArray);
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

public enum CosmosDbMinimumTlsVersion
{
    Tls,
    Tls11,
    Tls12
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