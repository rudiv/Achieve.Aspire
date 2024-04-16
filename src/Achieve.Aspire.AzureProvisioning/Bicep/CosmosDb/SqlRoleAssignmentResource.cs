using Achieve.Aspire.AzureProvisioning.Bicep.Internal;

namespace Achieve.Aspire.AzureProvisioning.Bicep.CosmosDb;

public class CosmosDbSqlRoleAssignmentResource : BicepResource
{
    private const string resourceType = "Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2023-11-15";

    public CosmosDbSqlRoleAssignmentResource(string name) : base(resourceType)
    {
        Name = name;
    }
    
    public CosmosDbAccountResource Parent { get; set; }
    public BicepResource Scope { get; set; }
    public BicepValue RoleDefinitionId { get; set; }
    public BicepValue PrincipalId { get; set; }

    public CosmosDbSqlRoleAssignmentResource WithParent(CosmosDbAccountResource account)
    {
        Parent = account;
        return this;
    }
    
    public CosmosDbSqlRoleAssignmentResource WithScope(CosmosDbAccountResource account)
    {
        Scope = account;
        return this;
    }
    
    public CosmosDbSqlRoleAssignmentResource WithScope(CosmosDbSqlDatabaseResource database)
    {
        Scope = database;
        return this;
    }
    
    public CosmosDbSqlRoleAssignmentResource WithScope(CosmosDbSqlContainerResource container)
    {
        Scope = container;
        return this;
    }

    public CosmosDbSqlRoleAssignmentResource WithBuiltInRole(CosmosDbSqlBuiltInRole role)
    {
        return role switch
        {
            CosmosDbSqlBuiltInRole.Reader => WithReaderRole(),
            CosmosDbSqlBuiltInRole.Contributor => WithContributorRole(),
            _ => throw new ArgumentOutOfRangeException(nameof(role), "Invalid Built in Role")
        };
    }

    public CosmosDbSqlRoleAssignmentResource WithReaderRole()
    {
        RoleDefinitionId = GetBaseBuiltInRolePrefix().WithArgument(new BicepStringValue("00000000-0000-0000-0000-000000000001"));
        return this;
    }
    
    public CosmosDbSqlRoleAssignmentResource WithContributorRole()
    {
        RoleDefinitionId = GetBaseBuiltInRolePrefix().WithArgument(new BicepStringValue("00000000-0000-0000-0000-000000000002"));
        return this;
    }

    // TODO Type this
    public CosmosDbSqlRoleAssignmentResource WithCustomRole(BicepResource resource)
    {
        RoleDefinitionId = new BicepPropertyAccessValue(new BicepVariableValue(resource.Name), "id");
        return this;
    }

    public CosmosDbSqlRoleAssignmentResource WithDefaultPrincipalId()
    {
        PrincipalId = new BicepVariableValue("principalId");
        return this;
    }

    private BicepVariableValue GetDatabaseAccountReference()
    {
        return Scope switch
        {
            CosmosDbAccountResource account => new BicepVariableValue(account.Name),
            CosmosDbSqlDatabaseResource database => new BicepVariableValue(database.Parent.Name),
            CosmosDbSqlContainerResource container => new BicepVariableValue(container.Parent.Parent.Name),
            _ => throw new InvalidOperationException("Could not determine scope for Cosmos DB Sql Role Assignment.")
        };
    }

    private BicepFunctionCallValue GetBaseBuiltInRolePrefix()
    {
        return new BicepFunctionCallValue("resourceId", new BicepStringValue("Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions"), 
            new BicepPropertyAccessValue(GetDatabaseAccountReference(), "name"));
    }

    public override void Construct()
    {
        Body.Add(new BicepResourceProperty("parent", GetDatabaseAccountReference()));
        Body.Add(new BicepResourceProperty("name", new BicepFunctionCallValue("guid", new BicepPropertyAccessValue(GetDatabaseAccountReference(), "id"), new BicepStringValue(Name))));
        var propertyBag = new BicepResourcePropertyBag("properties");
        propertyBag.AddProperty("roleDefinitionId", RoleDefinitionId);
        
        BicepValue scope;
        switch (Scope)
        {
            case CosmosDbAccountResource:
                scope = new BicepPropertyAccessValue(GetDatabaseAccountReference(), "id");
                break;
            case CosmosDbSqlDatabaseResource db:
                scope = new BicepInterpolatedString()
                    .Str(string.Empty)
                    .Exp(new BicepPropertyAccessValue(GetDatabaseAccountReference(), "id"))
                    .Str("/dbs/")
                    .Exp(new BicepPropertyAccessValue(new BicepVariableValue(db.Name), "name"))
                    .Str(string.Empty);
                break;
            case CosmosDbSqlContainerResource cn:
                scope = new BicepInterpolatedString()
                    .Str(string.Empty)
                    .Exp(new BicepPropertyAccessValue(GetDatabaseAccountReference(), "id"))
                    .Str("/dbs/")
                    .Exp(new BicepPropertyAccessValue(new BicepVariableValue(cn.Parent.Name), "name"))
                    .Str("/colls/")
                    .Exp(new BicepPropertyAccessValue(new BicepVariableValue(cn.Name), "name"))
                    .Str(string.Empty);
                break;
            default:
                throw new InvalidOperationException("Could not determine Scope for Role Assignment.");
        }
        
        propertyBag.AddProperty("scope", scope);
        propertyBag.AddProperty("principalId", PrincipalId);
        Body.Add(propertyBag);
    }
}

public enum CosmosDbSqlBuiltInRole
{
    Reader,
    Contributor
}