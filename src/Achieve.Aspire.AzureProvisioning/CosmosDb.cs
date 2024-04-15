using Achieve.Aspire.AzureProvisioning.Bicep.CosmosDb;
using Achieve.Aspire.AzureProvisioning.Bicep.Internal;
using Achieve.Aspire.AzureProvisioning.Resources;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning.Authorization;

namespace Achieve.Aspire.AzureProvisioning;

public static class CosmosDbExtensions
{
    /// <summary>
    /// Adds a Cosmos DB NoSQL Account to the application.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="name"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static IResourceBuilder<AzureCosmosDbResource>? AddAzureCosmosDbNoSqlAccount(this IDistributedApplicationBuilder builder, string name, Action<CosmosDbOptions> configure)
    {
        var accountResource = new CosmosDbAccountResource(name);
        var options = new CosmosDbOptions();
        configure(options);
        
        return builder.AddResource(new AzureCosmosDbResource(name, null));
    }
    
    public class AzureCosmosDbResource(string name, BicepFileOutput bicepFileOutput) : AchieveResource(name, bicepFileOutput)
    {
    }
}

public class CosmosDbOptions {
    public CosmosDbAccountResource DbAccount { get; set; }
    public Dictionary<string, CosmosDbDatabaseOptions> Databases { get; set; } = new();

    public CosmosDbDatabaseOptions AddDatabase(string name, Action<CosmosDbDatabaseResource> configure)
    {
        var database = new CosmosDbDatabaseResource(name);
        configure(database);
        Databases.Add(name, new CosmosDbDatabaseOptions(this, database));
        return Databases[name];
    }
}

public class CosmosDbDatabaseOptions(CosmosDbOptions parent, CosmosDbDatabaseResource database)
{
    public Dictionary<string, CosmosDbContainerResource> Containers { get; set; } = new();
    
    public CosmosDbContainerResource AddContainer(string name, Action<CosmosDbContainerResource> configure)
    {
        var container = new CosmosDbContainerResource(name);
        configure(container);
        Containers.Add(name, container);
        return container;
    }
}