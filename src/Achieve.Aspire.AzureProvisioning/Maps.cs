
using Achieve.Aspire.AzureProvisioning.Bicep.Batch;
using Achieve.Aspire.AzureProvisioning.Bicep.Internal;
using Achieve.Aspire.AzureProvisioning.Bicep.Maps;
using Achieve.Aspire.AzureProvisioning.Resources;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning;

namespace Achieve.Aspire.AzureProvisioning;

#pragma warning disable AZPROVISION001

public static class Maps
{
    public static IResourceBuilder<AzureMapsResource> AddAzureMapsAccount(this IDistributedApplicationBuilder builder, string name, Action<MapsAccountResource>? configure = null)
    {
        builder.AddAzureProvisioning();

        var accountResource = new MapsAccountResource(name);
        configure?.Invoke(accountResource);

        var fileOutput = BicepFileOutput.GetAspireFileOutput();
        fileOutput.AddResource(accountResource);
        fileOutput.AddOutput(new BicepOutput(AzureMapsResource.MapsClientId, BicepSupportedType.String, accountResource.Name + ".properties.uniqueId"));
        
        var resource = new AzureMapsResource(name, fileOutput, accountResource);
        var resourceBuilder = builder.AddResource(resource);

        return resourceBuilder.WithManifestPublishingCallback(resource.WriteToManifest);
    }
}


public class AzureMapsResource(string name, BicepFileOutput bicepFileOutput, BicepResource underlyingBicepResource)
    : AchieveResource(name, bicepFileOutput, underlyingBicepResource)
{
    public const string MapsClientId = "clientId";

    public BicepOutputReference ClientId => new(MapsClientId, this);
}

public static class MapsRoles
{
    public static RoleDefinition DataReader { get; } = new("423170ca-a8f6-4b0f-8487-9e4eb8f49bfa");
}