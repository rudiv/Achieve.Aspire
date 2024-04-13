# Aspire for the Real World

Eugh, love love love .NET Aspire.

In preview it's perfect right now for toy projects, but let's say we want to use it in the real world! It's... not
possible.

Well, it's not possible without `azd infra synth` and a _lot_ of manual editing. See [this discussion](https://github.com/Azure/azure-dev/discussions/3184) for some future direction.

Obligatory warning: This is a hack. For a preview product from Microsoft. Backed by legends (David Fowler & Damian
Edwards). Don't use it if you squirm at the thought of Reflection and other such trickery. Things **WILL** get better,
and this library **WILL NOT** be needed, hopefully very soon.

### What why

Aspire is very very opinionated and based around each of your containers/apps running as the same identity. Oh and
adding that identity as an Administrator / Global Access to everything it creates.

Unfortunately you can't override this behaviour even with the construct configuration in P5.

Anyway, let's obviously not do that in production. Just because 1 app might need access to something doesn't mean that
it, and all other apps should have access to everything.

### So what is this

This basically augments what's already there with proper, actually usable versions of the Aspire.Hosting.Azure.*
packages (at least the ones we need currently) that just forget about that whole "run everything as the same identity"
thing.

[My PR](https://github.com/dotnet/aspire/pull/3339) tries to go some way to do what this does, but it's rejected and no workaround will
be available for v1/GA, you're going to need to still do a tiny bit of manual editing on the container YAML template
files, or you can compile azd from [my branch here](https://github.com/rudiv/azure-dev/tree/aspire-project-uai) and it will magically work.

## What's supported

Very little, here's a list of stuff I want and will be here very soon.

- [x] (0.1.0) Managed Identities
- [x] (0.1.0) Key Vault Managed Identity
- [x] (0.1.0) Key Vault Secrets (from other Bicep variables)
- [x] (0.1.0) Add Managed Identity to Project
- [ ] Storage Managed Identity
- [ ] CosmosDB Full-Fidelity
- [ ] CosmosDB Managed Identity w/ SQL Roles

## How to use it

Add it! `Rudi.Dev.Aspire.Provisioning.RealWorld` on NuGet.

### Create your actual identities

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
    .WithManagedIdentity(id);
```