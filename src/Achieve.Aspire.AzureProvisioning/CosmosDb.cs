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

        if (options.Principals.Count > 0)
        {
            foreach(var (paramName, bicepOutputReference) in options.Principals)
            {
                fileOutput.AddParameter(new BicepParameter(paramName, BicepSupportedType.String));
            }
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
        
        if (options.RoleAssignments.Count > 0)
        {
            foreach (var roleAssignment in options.RoleAssignments)
            {
                fileOutput.AddResource(roleAssignment);
            }
        }
        
        fileOutput.AddOutput(new BicepOutput(AzureCosmosDbResource.AccountEndpointOutput, BicepSupportedType.String, accountResource.Name + ".properties.documentEndpoint"));

        var resource = new AzureCosmosDbResource(name, fileOutput);
        var resourceBuilder = builder.AddResource(resource);
        if (options.EnablePassPrincipalId)
        {
            resourceBuilder.WithParameter(AzureBicepResource.KnownParameters.PrincipalId);
        }
        if (options.Principals.Count > 0)
        {
            foreach(var (paramName, bicepOutputReference) in options.Principals)
            {
                resourceBuilder.WithParameter(paramName, bicepOutputReference);
            }
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

    public List<(string, BicepOutputReference)> Principals { get; set; } = [];

    public CosmosDbDatabaseOptions AddDatabase(string name, Action<CosmosDbSqlDatabaseResource>? configure = null)
    {
        var database = new CosmosDbSqlDatabaseResource(Resource, name);
        configure?.Invoke(database);
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
    
    /// <summary>
    /// Add a Role Assignment to the Cosmos DB Account.
    /// </summary>
    /// <param name="scope">Must be a <see cref="CosmosDbAccountResource" />, <see cref="CosmosDbSqlDatabaseResource"/> or <see cref="CosmosDbSqlContainerResource"/>.</param>
    /// <param name="output">Bicep Output Reference. Must be a Principal ID.</param>
    /// <param name="role">Built in role (currently) to assign.</param>
    /// <returns></returns>
    public CosmosDbAccountOptions WithRoleAssignment(BicepResource scope, BicepOutputReference output, CosmosDbSqlBuiltInRole role)
    {
        var paramName = output.Resource.Name + "Principal";
        if (Principals.All(p => p.Item1 != paramName))
        {
            Principals.Add((paramName, output));
        }
        var roleAssignment = new CosmosDbSqlRoleAssignmentResource(output.Resource.Name + "Ra_" + Helpers.StableIdentifier(output.Resource.Name + scope.Name + role));
        // Can't use WithScope as typed
        roleAssignment.Scope = scope;
        roleAssignment.WithBuiltInRole(role).PrincipalId = new BicepVariableValue(paramName);
        RoleAssignments.Add(roleAssignment);
        return this;
    }

    public CosmosDbAccountOptions WithRoleAssignment(BicepResource scope, IResourceBuilder<AzureManagedIdentityResource> identity, CosmosDbSqlBuiltInRole role) =>
        WithRoleAssignment(scope, identity.GetOutput("PrincipalId"), role);
}

public class CosmosDbDatabaseOptions(CosmosDbAccountOptions parent, CosmosDbSqlDatabaseResource sqlDatabase)
{
    public CosmosDbSqlDatabaseResource Resource { get; set; } = sqlDatabase;
    public Dictionary<string, CosmosDbSqlContainerResource> Containers { get; set; } = [];
    
    public CosmosDbSqlContainerResource AddContainer(string name, Action<CosmosDbSqlContainerResource>? configure = null)
    {
        var container = new CosmosDbSqlContainerResource(Resource, name);
        configure?.Invoke(container);
        Containers.Add(name, container);
        return container;
    }
}