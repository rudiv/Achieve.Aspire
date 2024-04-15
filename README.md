![Aspire Achieve](https://github.com/rudiv/Achieve.Aspire/blob/main/assets/aspire-achieve.png?raw=true)

# Achieve (for Aspire)

Achieve adds missing provisioning support to [.NET Aspire](https://github.com/dotnet/aspire) for real-world applications.

### What is .NET Aspire?

Aspire is an opinionated, cloud ready stack for building observable, production ready, distributed applications.

[Learn more](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview).

### What is Achieve?

Achieve augments Aspire at deployment time by adding replacements for the built in Aspire.Hosting.Azure.* packages that
allow for more real-world scenarios on proper applications that need to run on Azure.

## Why is Achieve needed?

To achieve (pun intended) real-world scenarios when using .NET Aspire (for now and at least GA release), you need to run
`azd infra synth` and manually edit the generated Bicep and YAML. The long term goal of Aspire is to allow for more
configuration within the AppHost, but right now that's simply not supported.

The primary issues that Achieve aims to solve are:

- The single identity / principal assigned to all projects by default
- The lack of finely grained control around Role Assignments in Azure
- Missing built-in Aspire Resources to configure Identities

## How to use it

Add it! `Achieve.Aspire.AzureProvisioning` on NuGet.

### Create your actual identities & resources

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

## What's supported

Very little, here's a list of stuff I want and will be here very soon.

- [x] (0.1.0) Managed Identities
- [x] (0.1.0) Key Vault Managed Identity
- [x] (0.1.0) Key Vault Secrets (from other Bicep variables)
- [x] (0.1.0) Add Managed Identity to Project
- [ ] Storage Managed Identity
- [ ] CosmosDB Full-Fidelity
- [ ] CosmosDB Managed Identity w/ SQL Roles

Note that very much what isn't supported right now is local dev against these resources. The resources created here will
only work at publishing type.

Wrap these resources in publish mode like this:
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