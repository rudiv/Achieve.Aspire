using Achieve.Aspire.AzureProvisioning.Bicep.Internal;
using Bicep.Core.Syntax;

namespace Achieve.Aspire.AzureProvisioning.Bicep.CosmosDb;

public class CosmosDbSqlContainerResource : BicepResource
{
    private const string resourceType = "Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-11-15";
    
    public CosmosDbSqlDatabaseResource Parent { get; set; }
    
    public CosmosDbSqlContainerResource(CosmosDbSqlDatabaseResource parent, string name) : base(resourceType)
    {
        Parent = parent;
        Name = name;
    }
    
    public int? AnalyticalStorageTtl { get; set; }
    
    public int? AutoscaleMaxThroughput { get; set; }
    public int? Throughput { get; set; }
    
    public int? DefaultTtlInSeconds { get; set; }
    
    public List<CosmosDbSqlContainerComputedProperty> ComputedProperties { get; set; } = [];
    
    public CosmosDbSqlContainerPartitionKey? PartitionKey { get; set; }

    /// <summary>
    /// The default indexing policy (null here) includes every path in the content of the documents within the container.
    ///
    /// You will need to define at least includedPaths and excludedPaths.
    /// </summary>
    public CosmosDbSqlContainerIndexingPolicy? IndexingPolicy { get; set; } = null;
    
    public List<CosmosDbSqlContainerUniqueKey> UniqueKeyPolicy { get; set; } = [];

    protected override void ValidateResourceType()
    {
        if (PartitionKey == null)
        {
            throw new InvalidOperationException("You must define a PartitionKey for a container.");
        }
        if (Parent.Parent.Capabilities.Contains(CosmosDbAccountResource.CapabilityServerless))
        {
            if (Throughput != null || AutoscaleMaxThroughput != null)
            {
                throw new InvalidOperationException("Cannot set throughput on Container when Account is Serverless.");
            }
        } else if (Throughput != null && AutoscaleMaxThroughput != null)
        {
            throw new InvalidOperationException("Cannot set both Throughput and AutoscaleMaxThroughput on Container.");
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
        
        if (AnalyticalStorageTtl != null) resourceBag.AddProperty("analyticalStorageTtl", new BicepIntValue(AnalyticalStorageTtl.Value));
        if (ComputedProperties.Count > 0)
        {
            var computedPropertyBag = new BicepResourcePropertyArray("computedProperties");
            foreach (var computedProperty in ComputedProperties)
            {
                computedPropertyBag.AddValue(new BicepResourcePropertyBag("cp").AsValueOnly()
                    .AddProperty("name", new BicepStringValue(computedProperty.Name))
                    .AddProperty("query", new BicepStringValue(computedProperty.Query)));
            }
            resourceBag.AddProperty(computedPropertyBag);
        }
        if (DefaultTtlInSeconds != null) resourceBag.AddProperty("defaultTtl", new BicepIntValue(DefaultTtlInSeconds.Value));
        if (IndexingPolicy != null) resourceBag.AddProperty(IndexingPolicy);
        resourceBag.AddProperty(PartitionKey!);
        if (UniqueKeyPolicy.Count > 0)
        {
            var uniqueKeyPolicyBag = new BicepResourcePropertyBag("uniqueKeyPolicy", 3);
            var uniqueKeysArray = new BicepResourcePropertyArray("uniqueKeys", 4);
            foreach (var uniqueKey in UniqueKeyPolicy)
            {
                uniqueKeysArray.AddValue(uniqueKey);
            }
            uniqueKeyPolicyBag.AddProperty(uniqueKeysArray);
            resourceBag.AddProperty(uniqueKeyPolicyBag);
        }
        
        propertyBag.AddProperty(resourceBag);
        Body.Add(propertyBag);
    }
}

public class CosmosDbSqlContainerPartitionKey : IBicepResourceProperty
{
    public List<string> Paths { get; set; }

    public CosmosDbSqlContainerPartitionKey(params string[] paths)
    {
        Paths = new(paths);
    }

    public SyntaxBase ToBicepSyntax()
    {
        var partitionKeyBag = new BicepResourcePropertyBag("partitionKey", 3);

        if (Paths.Count == 1)
        {
            partitionKeyBag.AddProperty("kind", new BicepStringValue("Hash"));
        }
        else
        {
            partitionKeyBag.AddProperty("kind", new BicepStringValue("MultiHash"));
            partitionKeyBag.AddProperty("version", new BicepIntValue(2));
        }
        
        var pathsArray = new BicepResourcePropertyArray("paths", 4);
        foreach (var path in Paths)
        {
            pathsArray.AddValue(new BicepStringValue(path));
        }

        partitionKeyBag.AddProperty(pathsArray);

        return partitionKeyBag.ToBicepSyntax();
    }
}

public class CosmosDbSqlContainerUniqueKey(params string[] paths) : IBicepResourceProperty
{
    public SyntaxBase ToBicepSyntax()
    {
        var uniqueKeyBag = new BicepResourcePropertyBag("uk", 5).AsValueOnly();
        var pathsArray = new BicepResourcePropertyArray("paths", 6);
        foreach (var path in paths)
        {
            pathsArray.AddValue(new BicepStringValue(path));
        }
        uniqueKeyBag.AddProperty(pathsArray);
        return uniqueKeyBag.ToBicepSyntax();
    }
}

public class CosmosDbSqlContainerIndexingPolicy : IBicepResourceProperty
{
    public bool Automatic { get; set; } = true;
    public List<CosmosDbSqlContainerCompositeIndex> CompositeIndexes { get; set; } = [];
    public List<CosmosDbSqlContainerIncludedPath> IncludedPaths { get; set; } = [];
    public List<string> ExcludedPaths { get; set; } = [];
    public CosmosDbSqlContainerIndexingMode IndexingMode { get; set; } = CosmosDbSqlContainerIndexingMode.Consistent;

    public SyntaxBase ToBicepSyntax()
    {
        var indexingPolicyBag = new BicepResourcePropertyBag("indexingPolicy", 3);
        indexingPolicyBag.AddProperty("indexingMode", new BicepStringValue(IndexingMode.ToString().ToLowerInvariant()));
        indexingPolicyBag.AddProperty("automatic", new BicepBooleanValue(Automatic));

        if (CompositeIndexes.Count > 0)
        {
            var compositeIndexesArray = new BicepResourcePropertyArray("compositeIndexes", 4);
            foreach (var compositeIndex in CompositeIndexes)
            {
                var compositeIndexBag = new BicepResourcePropertyBag("ci", 5).AsValueOnly();
                var pathsArray = new BicepResourcePropertyArray("paths", 6);
                foreach (var path in compositeIndex.Paths)
                {
                    pathsArray.AddValue(new BicepResourcePropertyBag("path", 7)
                        .AddProperty("path", new BicepStringValue(path.Path))
                        .AddProperty("order", new BicepStringValue(path.Order.ToString().ToLowerInvariant())));
                }
                compositeIndexBag.AddProperty(pathsArray);
                compositeIndexesArray.AddValue(compositeIndexBag);
            }
            indexingPolicyBag.AddProperty(compositeIndexesArray);
        }
        
        var excludedPathsArray = new BicepResourcePropertyArray("excludedPaths", 4);
        foreach (var excludedPath in ExcludedPaths)
        {
            var excludedValueBag = new BicepResourcePropertyBag("ep", 5).AsValueOnly()
                .AddProperty("path", new BicepStringValue(excludedPath));
            excludedPathsArray.AddValue(excludedValueBag);
        }
        indexingPolicyBag.AddProperty(excludedPathsArray);

        var includedPathsArray = new BicepResourcePropertyArray("includedPaths", 4);
        foreach (var includedPath in IncludedPaths)
        {
            includedPathsArray.AddValue(includedPath);
        }
        indexingPolicyBag.AddProperty(includedPathsArray);

        return indexingPolicyBag.ToBicepSyntax();
    }
}

public class CosmosDbSqlContainerComputedProperty(string name, string query)
{
    public string Name { get; set; } = name;
    public string Query { get; set; } = query;
}

public class CosmosDbSqlContainerCompositeIndex
{
    public List<CosmosDbSqlContainerCompositeIndexPath> Paths { get; set; } = [];
}

public class CosmosDbSqlContainerCompositeIndexPath(string path, CosmosDbSqlContainerCompositeIndexPathOrder order)
{
    public string Path { get; set; } = path;
    public CosmosDbSqlContainerCompositeIndexPathOrder Order { get; set; } = order;
}

public enum CosmosDbSqlContainerCompositeIndexPathOrder
{
    Ascending,
    Descending
}
    
public class CosmosDbSqlContainerIncludedPath(string path) : IBicepResourceProperty
{
    public string Path { get; set; } = path;
    public List<CosmosDbSqlContainerIncludedPathIndex> Indexes { get; set; } = [];

    public SyntaxBase ToBicepSyntax()
    {
        var includedPathObj = new BicepResourcePropertyBag("ip", 5).AsValueOnly()
            .AddProperty("path", new BicepStringValue(Path));
        if (Indexes.Count > 0)
        {
            var indexesArray = new BicepResourcePropertyArray("indexes", 6);
            foreach (var index in Indexes)
            {
                var indexBag = new BicepResourcePropertyBag("ix", 7).AsValueOnly();
                if (index.DataType != null) indexBag.AddProperty("dataType", new BicepStringValue(index.DataType.ToString()!));
                if (index.Kind != null) indexBag.AddProperty("kind", new BicepStringValue(index.Kind.ToString()!));
                if (index.Precision != null) indexBag.AddProperty("precision", new BicepIntValue(index.Precision.Value));
                indexesArray.AddValue(indexBag);
            }
        }

        return includedPathObj.ToBicepSyntax();
    }
}

public class CosmosDbSqlContainerIncludedPathIndex
{
    public CosmosDbSqlContainerIncludedPathIndexDataType? DataType { get; set; }
    public CosmosDbSqlContainerIncludedPathIndexKind? Kind { get; set; }
    public int? Precision { get; set; }
}

public enum CosmosDbSqlContainerIncludedPathIndexDataType
{
    LineString,
    MultiPolygon,
    Number,
    Point,
    Polygon,
    String
}

public enum CosmosDbSqlContainerIncludedPathIndexKind
{
    Hash,
    Range,
    Spatial
}

public enum CosmosDbSqlContainerIndexingMode
{
    Consistent,
    Lazy,
    None
}