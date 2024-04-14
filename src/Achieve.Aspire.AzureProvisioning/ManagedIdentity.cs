using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Publishing;
using Azure.Provisioning.ManagedServiceIdentities;

namespace Achieve.Aspire.AzureProvisioning;

public static class ManagedIdentityExtensions
{
    public const string ResourceId = "resourceId";
    public const string PrincipalId = "principalId";

    public const string ClientId = "clientId";
    // For consumption by https://github.com/rudiv/azure-dev/tree/aspire-project-uai
    // Gets wired up in the YAML
    public const string AzdUaiDetection = "userAssignedIdentities";

    private static MethodInfo? wpOriginal;
    
    static ManagedIdentityExtensions()
    {
        wpOriginal = typeof(ManifestPublishingContext).GetMethod("WriteProjectAsync", BindingFlags.NonPublic | BindingFlags.Instance);

        if (wpOriginal == null)
        {
            throw new InvalidOperationException(nameof(ManifestPublishingContext) + ".WriteProjectAsync(p) not found! Aspire may have changed.");
        }
    }
    
    public static IResourceBuilder<AzureManagedIdentityResource> AddManagedIdentity(this IDistributedApplicationBuilder builder, string name)
    {
        // TODO - Check if they fix this.
        if (name.Any(m => m == '-'))
        {
            throw new ArgumentException($"Names with dashes currently break Bicep generation. Please change {name} to remove it.", nameof(name));
        }

        builder.AddAzureProvisioning();
        var configureConstruct = (ResourceModuleConstruct c) =>
        {
            var id = new UserAssignedIdentity(c, name: name);
            id.AddOutput(PrincipalId, i => i.PrincipalId);
            id.AddOutput(ClientId, i => i.ClientId);
            id.AddOutput(ResourceId, i => i.Id);
        };
        
        var resource = new AzureManagedIdentityResource(name, configureConstruct);

        return builder.AddResource(resource)
            .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    /// <summary>
    /// Adds a managed identity to the project resource.
    /// </summary>
    /// <param name="builder">Project Resource</param>
    /// <param name="env">Environment variable prefix (created as {ENV}_CLIENTID)</param>
    /// <param name="identity">Identity to add</param>
    /// <returns><see cref="builder" /></returns>
    public static IResourceBuilder<ProjectResource> WithManagedIdentity(this IResourceBuilder<ProjectResource> builder,
        string env,
        IResourceBuilder<AzureManagedIdentityResource> identity)
    {
        builder.WithManifestPublishingCallback(c =>
        {
            wpOriginal!.Invoke(c, [builder.Resource]);
            c.Writer.WritePropertyName("UserAssignedIdentities");
            c.Writer.WriteStartArray();
            JsonSerializer.Serialize(c.Writer,
                new UserAssignedIdentityDescriptor(env, identity.GetOutput(ClientId).ValueExpression, identity.GetOutput(ResourceId).ValueExpression));
            c.Writer.WriteEndArray();
        });

        return builder;
    }
}
public class AzureManagedIdentityResource(string name, Action<ResourceModuleConstruct> configureConstruct) : AzureConstructResource(name, configureConstruct)
{
    public BicepOutputReference PrincipalId => new(ManagedIdentityExtensions.PrincipalId, this);
}

public sealed record UserAssignedIdentityDescriptor
{
    /// <summary>
    /// Identity Client ID.
    /// </summary>
    [JsonPropertyName("clientId")]
    public string ClientId { get; init; }
    /// <summary>
    /// Identity Resource ID.
    /// </summary>
    [JsonPropertyName("resourceId")]
    public string IdentityResourceId { get; init; }
    /// <summary>
    /// Environment Variable Prefix.
    /// </summary>
    [JsonPropertyName("env")]
    public string EnvironmentVariablePrefix { get; init; }

    /// <summary>
    /// Creates a new <see cref="UserAssignedIdentityDescriptor"/>.
    /// </summary>
    /// <param name="envPrefix">Environment Variable prefix for the Client ID.</param>
    /// <param name="clientId">The identity's Client ID for usage within the app.</param>
    /// <param name="identityResourceId">The identity Resource ID for assignment to the container app.</param>
    public UserAssignedIdentityDescriptor(string envPrefix, string clientId, string identityResourceId) => (ClientId, IdentityResourceId, EnvironmentVariablePrefix) = (clientId, identityResourceId, envPrefix);
}