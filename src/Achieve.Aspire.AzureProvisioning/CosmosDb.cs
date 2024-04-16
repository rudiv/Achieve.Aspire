using Achieve.Aspire.AzureProvisioning.Bicep.CosmosDb;
using Achieve.Aspire.AzureProvisioning.Bicep.Internal;
using Achieve.Aspire.AzureProvisioning.Resources;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;

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
    public static IResourceBuilder<AzureCosmosDbResource> AddAzureCosmosDbNoSqlAccount(this IDistributedApplicationBuilder builder, string name, Action<CosmosDbAccountOptions> configure)
    {
        var accountResource = new CosmosDbAccountResource(name);
        var options = new CosmosDbAccountOptions(accountResource);
        configure(options);

        var fileOutput = BicepFileOutput.GetAspireFileOutput();
        if (options.EnablePassPrincipalId)
        {
            fileOutput.AddParameter(new BicepParameter(AzureBicepResource.KnownParameters.PrincipalId, BicepSupportedType.String));
        }
        
        fileOutput.AddResource(accountResource);
        foreach (var database in options.Databases)
        {
            fileOutput.AddResource(database.Value.Resource);
            foreach (var container in database.Value.Containers)
            {
                fileOutput.AddResource(container.Value);
            }
        }
        
        fileOutput.AddOutput(new BicepOutput(AzureCosmosDbResource.AccountEndpointOutput, BicepSupportedType.String, accountResource.Name + ".properties.documentEndpoint"));

        var resource = new AzureCosmosDbResource(name, fileOutput);
        var resourceBuilder = builder.AddResource(resource);
        if (options.EnablePassPrincipalId)
        {
            resourceBuilder.WithParameter(AzureBicepResource.KnownParameters.PrincipalId);
        }
        return resourceBuilder.WithManifestPublishingCallback(resource.WriteToManifest);
    }
    
    public class AzureCosmosDbResource(string name, BicepFileOutput bicepFileOutput) : AchieveResource(name, bicepFileOutput), IResourceWithConnectionString
    {
        public const string AccountEndpointOutput = "accountEndpoint";
        
        public BicepOutputReference AccountEndpoint => new(AccountEndpointOutput, this);
        
        
        /// <summary>
        /// Gets the connection string template for the manifest for the Azure Cosmos DB resource.
        /// </summary>
        public ReferenceExpression ConnectionStringExpression => ReferenceExpression.Create($"{AccountEndpoint}");
    }
}

public class CosmosDbAccountOptions(CosmosDbAccountResource resource)
{
    public CosmosDbAccountResource Resource { get; set; } = resource;
    public Dictionary<string, CosmosDbDatabaseOptions> Databases { get; set; } = [];
    
    public List<CosmosDbSqlRoleAssignmentResource> RoleAssignments { get; set; } = [];
    
    public bool EnablePassPrincipalId { get; set; }

    public CosmosDbDatabaseOptions AddDatabase(string name, Action<CosmosDbSqlDatabaseResource> configure)
    {
        var database = new CosmosDbSqlDatabaseResource(Resource, name);
        configure(database);
        Databases.Add(name, new CosmosDbDatabaseOptions(this, database));
        return Databases[name];
    }

    public CosmosDbAccountOptions WithDevelopmentGlobalAccess()
    {
        EnablePassPrincipalId = true;
        RoleAssignments.Add(new CosmosDbSqlRoleAssignmentResource("developmentAccess")
            .WithScope(Resource)
            .WithDefaultPrincipalId()
            .WithContributorRole());
        return this;
    }
}

public class CosmosDbDatabaseOptions(CosmosDbAccountOptions parent, CosmosDbSqlDatabaseResource sqlDatabase)
{
    public CosmosDbSqlDatabaseResource Resource { get; set; } = sqlDatabase;
    public Dictionary<string, CosmosDbSqlContainerResource> Containers { get; set; } = [];
    
    public CosmosDbSqlContainerResource AddContainer(string name, Action<CosmosDbSqlContainerResource> configure)
    {
        var container = new CosmosDbSqlContainerResource(Resource, name);
        configure(container);
        Containers.Add(name, container);
        return container;
    }
}