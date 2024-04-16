![Aspire Achieve](https://github.com/rudiv/Achieve.Aspire/blob/main/assets/aspire-achieve.png?raw=true)

# Achieve (for Aspire)

Achieve adds missing provisioning support to [.NET Aspire](https://github.com/dotnet/aspire) for real-world applications.

### What is .NET Aspire?

Aspire is an opinionated, cloud ready stack for building observable, production ready, distributed applications.

[Learn more](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview).

### What is Achieve?

Achieve augments Aspire at deployment time by adding replacements for the built in Aspire.Hosting.Azure.* packages that
allow for more real-world scenarios on proper applications that need to run on Azure.

It allows you to define Resources fully from C#, without having to use `azd infra synth` on your Aspire projects or
manually create Bicep. You can also create your own Resources for ones that aren't directly supported by Achieve in a
standardised manner. See the [Bicep Generators](https://github.com/rudiv/Achieve.Aspire/tree/main/src/Achieve.Aspire.AzureProvisioning/Bicep) 
for more information and examples on how to do this.

**Achieve should not have to exist.** It exists because of gaps in the Aspire stack that are not yet filled, and I hope
that with time, Achieve will become redundant as Aspire matures.

## Why is Achieve needed?

To achieve (pun intended) real-world scenarios when using .NET Aspire (for now and at least GA release), you need to run
`azd infra synth` and manually edit the generated Bicep and YAML. The long term goal of Aspire is to allow for more
configuration within the AppHost, but right now that's simply not supported.

There's a discussion I raised [on the azd repository](https://github.com/Azure/azure-dev/discussions/3184) with a lot more detail, where
David Fowler repeatedly points out that `azd infra synth` should be a "last resort". Unfortunately in its current state, it's the only
way to get resources that are deployable to the real-world for production purposes. On the Managed Identity front, there's a
[pull request on Aspire](https://github.com/dotnet/aspire/pull/3339) that adds some basic functionality (but is sat rejected as
there are already future ideas in this space).

The primary issues that Achieve aims to solve are:

- The single identity / principal assigned to all projects by default
- The lack of finely grained control around Role Assignments in Azure
- Missing Resources in Aspire.Hosting.Azure.* (Azure.Provisioning) that are needed for real-world applications
- Full descriptions of resources that aren't possible using Aspire.Hosting.Azure.*

An example of the last point would be Cosmos DB. Aspire.Hosting.Azure.CosmosDB only allows you to set the account up with
databases, but not configure the Containers within it nor access to the data plane. I can only assume they expect people
to use the Control Plane via SDK, which is a terrible idea as these are deployment related resources. 

## How to use it

Add it! `Achieve.Aspire.AzureProvisioning` on NuGet.

Achieve adds new methods to describe resources, along with specific configuration options that allow you to describe
resources more cleanly than manually setting properties within Azure.Provisioning.

An example of this is the Key Vault builder below, where we can add a Managed Identity simply by referencing it and the
role.

### Create your Achieve Resources

```csharp
// NOTE - Don't use hyphens in this, it will partially break Bicep generation despite "Name must contain only ASCII letters, digits, and hyphens."
var id = builder.AddManagedIdentity("myidentity");

// Key Vault Zero Trust
var kv = builder.AddZtAzureKeyVault("mykv", b => {
    b.AddManagedIdentity(id, KeyVaultRoles.SecretsUser);
});

// Add a Managed Identity to a project
// Note this just outputs it to the manifest, you will need to update the YAML or use the azd branch above
builder.AddProject<Projects.MyProject>("myproject")
    .WithManagedIdentity("MYID", id);

// You can also add Role Assignments to resources manually (currently only KV supported)
builder.AddRoleAssignment(kv, id, KeyVaultRoles.SecretsUser);
```

#### Creating a Cosmos DB Resource

With Achieve, you can describe your Cosmos DB resources in full, including the databases, containers, and access to them.

Minimal example below, though you can configure most aspects of the actual Bicep resource.

```csharp
var cosmos = builder.AddAzureCosmosDbNoSqlAccount("cosmos", acc =>
{
    // During Development, you can either use the emulator (suggest using the Aspire.Hosting.Azure.CosmosDB package), or
    // provision it in the cloud with development defaults (public access, serverless, etc).  
    if (builder.ExecutionContext.IsRunMode) {
        ac.Resource.WithDevelopmentDefaults();
        ac.WithDevelopmentGlobalAccess(); // Adds your local principal to have access to everything in the account
    }
    acc.AddDatabase("db", db => { }).AddContainer("cn", cn =>
    {
        cn.PartitionKey = new CosmosDbSqlContainerPartitionKey("/id");
    });
});
```

You can use this without issue with the Aspire Component for Cosmos DB. Just remember you need to wire up the correct
credential to access the database.

AppHost:

```csharp
builder.AddProject<Projects.MyProject>("myproject")
    .WithReference(cosmos);
```

Project Startup:

```csharp
builder.AddAzureCosmosDBClient("cosmos",
    configureSettings: o =>
    {
        // If using AZURE_CLIENT_ID, or in local development to use your az cli credential
        o.Credential = new DefaultAzureCredential();
        // If using the non-default
        //o.Credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = "MYID_CLIENT_ID" });
    });
```

### Supported Resources

- [x] (0.1.0 - live) Microsoft.ManagedIdentity/userAssignedIdentities*
- [x] (0.1.0 - live) Microsoft.KeyVault/vaults*
- [x] (0.1.0 - live) Microsoft.KeyVault/vaults/secrets*
- [x] (0.1.0 - live) Microsoft.Authorization/roleAssignments
- [ ] (0.2.X) Microsoft.Storage/storageAccounts
- [ ] (0.2.X) Microsoft.Storage/storageAccounts/blobServices
- [ ] (0.2.X) Microsoft.Storage/storageAccounts/blobServices/containers
- [ ] (0.2.X) Microsoft.Storage/storageAccounts/queueServices
- [ ] (0.2.X) Microsoft.Storage/storageAccounts/queueServices/queues
- [ ] (0.2.X) Microsoft.Storage/storageAccounts/tableServices
- [ ] (0.2.X) Microsoft.Storage/storageAccounts/tableServices/tables
- [x] (0.2.0 - live) Microsoft.DocumentDB/databaseAccounts (NoSQL only)
- [x] (0.2.0 - live) Microsoft.DocumentDB/databaseAccounts/sqlDatabases
- [x] (0.2.0 - live) Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers
- [x] (0.2.0 - live) Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments
- [ ] (0.2.X) Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions
- [ ] (0.2.X) Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/storedProcedures
- [ ] (0.2.X) Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/triggers
- [ ] (0.2.X) Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/userDefinedFunctions

* Denotes that support for these resources is implemented via Azure.Provisioning.

## Current State / Version

This project is brand new, but supports something that both I and my team have needed for a long time. I'd hoped to get
certain parts of this in to Aspire directly as that's what makes sense, but they have their own plans to deliver similar
functionality after GA.

With that said, Achieve aims to support some of the most common resources that are required and polyfill them in to Aspire
until such time as they are natively supported. It doesn't support all resources (see above), but if you have a need for
them I'm more than happy to accept PRs or look at implementing things myself, just raise an issue.

- 0.1.0 - Minimal to support the addition of Managed Identities as well as the Custom ID support.
- 0.2.0 - Added Bicep generator for the creation of more complex resources. Added Cosmos DB and Role Assignment support.
- 0.2.X - More resources (see above). Tidy up/unify the APIs a little.
- 0.3.0 - Add a tool to complement `azd` so that the below is not required.
- 0.4.0 - Use above tool to also allow full customisation of the generated Bicep templates, down to the Container App Environment.
- X.X.X - ???

## Assigning Managed Identity to Projects

As above, you can call `.WithManagedIdentity("MYID", id);` after your `AddProject`, which will generate custom metadata
within the Aspire manifest. This metadata can be used to assign the managed identity to the project inside the generated
templates, but it requires a custom build of azd to do that (below). Alternatively, you can manually edit the generated
yaml templates, though this requires you falling back down to `azd infra synth`.

### (If comfortable using custom azd) Build custom azd

My branch of azd knows how to wire up the managed identity to the project by adding support for a "userAssignedIdentities"
key to the Aspire manifest.

[View / Clone the branch from here](https://github.com/rudiv/azure-dev/tree/aspire-project-uai)

Then, simply run your own compiled version of `azd` against your project as you would normally (`azd up`, etc).

### (If not using custom azd) Update Bicep / YAML Resources

You'll need to `azd infra synth`.

In the `main.bicep` file, add the following to the end with the other exports:

```
output MYIDENTITY_CLIENTID string = myidentity.outputs.clientId
output MYIDENTITY_RESOURCEID string = myidentity.outputs.ResourceId
```

In each project's `containerapp.tmpl.yaml`, add the Resource ID to the `userAssignedIdentities`, eg:

```yaml
identity:
  type: UserAssigned
  userAssignedIdentities:
    '{{ .Env.MYIDENTITY_RESOURCEID }}': {}
    ? "{{ .Env.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID }}"
    : {}
```

You can (and also should) probably remove the default managed identity.

Then add the client ID to the environment variables, eg:

```yaml
      env:
      - name: AZURE_CLIENT_ID
        value: {{ .Env.MANAGED_IDENTITY_CLIENT_ID }}
      - name: ASPNETCORE_FORWARDEDHEADERS_ENABLED
        value: "true"
      - name: MYID_CLIENT_ID
        value: '{{ .Env.MYIDENTITY_CLIENTID }}'
```

## Usage in Apps

You can then create a `DefaultAzureCredential` with the Client ID from `MYID_CLIENT_ID` for use. Alternatively, if you're
removing the default terribleness, just call it `AZURE_CLIENT_ID` and `DefaultAzureCredential` will use this automatically.

You can do this automatically by changing the `.WithManagedIdentity("MYID", id)` to `.WithManagedIdentity("AZURE", id)`.

**Important** Even when using the custom `azd` as above, this won't remove the default assignment. You'll still need to
do that manually.

## Usage at Development Time

Local Development against most of the resources that Achieve will support is only partially possible, as we're describing
publish-time only security definitions for the most part.

At development time, it may be best to use the Aspire provided resources to just configure them in full-trust for your
development account, using Achieve provided resources at Publish time.

You can do this as follows:
```csharp
IResourceBuilder<AzureKeyVaultResource> kv;
if (builder.ExecutionContext.IsPublishMode)
{
    kv = builder.AddAzureKeyVault(...);
}
else
{
    var id = builder.AddManagedIdentity(..);
    kv = builder.AddZtAzureKeyVault(..);
}
```