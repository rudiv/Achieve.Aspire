using Achieve.Aspire.AzureProvisioning.Bicep.Internal;
using Achieve.Aspire.AzureProvisioning.Extensions;

namespace Achieve.Aspire.AzureProvisioning.Bicep.Batch;

public sealed class BatchCertificateResource : BicepResource
{
    private const string resourceType = "Microsoft.Batch/batchAccounts/certificates@2023-11-01";
    
    public BatchAccountResource Parent { get; set; }
    
    public string Data { get; set; }

    /// <summary>
    /// The format of the certificate.
    /// </summary>
    public CertificateFormat Format { get; set; } = CertificateFormat.Pfx;
    
    public string? Password { get; set; }
    
    public string Thumbprint { get; set; }
    
    public string ThumbprintAlgorithm => "SHA1";
    

    public BatchCertificateResource(BatchAccountResource parent, string name) : base(resourceType)
    {
        Parent = parent;
        Name = name;
    }

    protected override void ValidateResourceType()
    {
        if (!Name.MatchesConstraints(3, 45,
                StringExtensions.CharacterClass.Alphanumeric | StringExtensions.CharacterClass.Underscore |
                StringExtensions.CharacterClass.Hyphen))
        {
            throw new InvalidOperationException(
                "Name must be between 5 and 45 characters long and can only contain alphanumerics, underscores and hyphens");
        }

        if (Format == CertificateFormat.Cer && Password is not null)
        {
            throw new InvalidOperationException("The password must not be specified if the certificate format is Cer");
        }

        if (string.IsNullOrWhiteSpace(Data) || Data.Length > 10000) //Unclear if this is a metric or binary limit
        {
            throw new InvalidOperationException("The maximum length of the certificate is 10KB");
        }
    }
    
    public override void Construct()
    {
        Body.Add(new BicepResourceProperty("parent", new BicepVariableValue(Parent.Name)));
        Body.Add(new BicepResourceProperty("name", new BicepStringValue(Name)));

        var propertyBag = new BicepResourcePropertyBag(BicepResourceProperties.Properties);
        propertyBag.AddProperty("data", new BicepStringValue(Data));
        propertyBag.AddProperty("format", new BicepStringValue(Format.GetValueFromEnumMember()));
        if (Format == CertificateFormat.Pfx)
        {
            propertyBag.AddProperty("password", new BicepStringValue(Password ?? ""));
        }

        propertyBag.AddProperty("thumbprint", new BicepStringValue(Thumbprint));
        propertyBag.AddProperty("thumbprintAlgorithm", new BicepStringValue(ThumbprintAlgorithm));
        Body.Add(propertyBag);
    }
}

public enum CertificateFormat
{
    Cer,
    Pfx
}
