
using Achieve.Aspire.AzureProvisioning.Bicep.Batch;
using Achieve.Aspire.AzureProvisioning.Bicep.Internal;
using Achieve.Aspire.AzureProvisioning.Resources;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;

namespace Achieve.Aspire.AzureProvisioning;

public static class Batch
{
    public static IResourceBuilder<AzureBatchResource> AddAzureBatchAccount(this IDistributedApplicationBuilder builder, string name, Action<BatchAccountOptions> configure)
    {
        builder.AddAzureProvisioning();

        var accountResource = new BatchAccountResource(name);
        var options = new BatchAccountOptions(accountResource);
        configure(options);

        var fileOutput = BicepFileOutput.GetAspireFileOutput();
        fileOutput.AddResource(accountResource);
        foreach (var certificate in options.Certificates)
        {
            fileOutput.AddResource(certificate.Value.Resource);
        }
        
        var resource = new AzureBatchResource(name, fileOutput, accountResource);
        var resourceBuilder = builder.AddResource(resource);

        return resourceBuilder.WithManifestPublishingCallback(resource.WriteToManifest);
    }
    
    public class AzureBatchResource(string name, BicepFileOutput bicepFileOutput, BicepResource underlyingBicepResource)
        : AchieveResource(name, bicepFileOutput, underlyingBicepResource)
    {
        public const string BatchResourceName = "resourceName";

        public BicepOutputReference AccountEndpoint => new(BatchResourceName, this);
    }
}

public class BatchAccountOptions(BatchAccountResource resource)
{
    public BatchAccountResource Resource { get; set; } = resource;

    public Dictionary<string, BatchCertificateOptions> Certificates { get; set; } = [];

    public BatchCertificateOptions AddCertificate(string name, Action<BatchCertificateResource>? configure = null)
    {
        var certificate = new BatchCertificateResource(Resource, name);
        configure?.Invoke(certificate);
        Certificates.Add(name, new BatchCertificateOptions(this, certificate));
        return Certificates[name];
    }
}

public class BatchCertificateOptions(BatchAccountOptions parent, BatchCertificateResource certificate)
{
    public BatchCertificateResource Resource { get; set; } = certificate;
}