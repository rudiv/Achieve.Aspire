using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Achieve.Aspire.AzureProvisioning.Tests.Utils;


/// <summary>
/// DistributedApplication.CreateBuilder() creates a builder that includes configuration to read from appsettings.json.
/// The builder has a FileSystemWatcher, which can't be cleaned up unless a DistributedApplication is built and disposed.
/// This class wraps the builder and provides a way to automatically dispose it to prevent test failures from excessive
/// FileSystemWatcher instances from many tests.
/// </summary>
public sealed class TestDistributedApplicationBuilder : IDisposable, IDistributedApplicationBuilder
{
    private readonly IDistributedApplicationBuilder innerBuilder;
    private bool builtApp;

    public ConfigurationManager Configuration => innerBuilder.Configuration;
    public string AppHostDirectory => innerBuilder.AppHostDirectory;
    public IHostEnvironment Environment => innerBuilder.Environment;
    public IServiceCollection Services => innerBuilder.Services;
    public DistributedApplicationExecutionContext ExecutionContext => innerBuilder.ExecutionContext;
    public IResourceCollection Resources => innerBuilder.Resources;

    public static TestDistributedApplicationBuilder Create(DistributedApplicationOperation operation = DistributedApplicationOperation.Run)
    {
        if (operation == DistributedApplicationOperation.Publish)
        {
            var options = new DistributedApplicationOptions
            {
                Args = ["Publishing:Publisher=manifest"]
            };
            return new(DistributedApplication.CreateBuilder(options));
        }

        return new(DistributedApplication.CreateBuilder());
    }

    private TestDistributedApplicationBuilder(IDistributedApplicationBuilder builder)
    {
        innerBuilder = builder;
    }

    public IResourceBuilder<T> AddResource<T>(T resource) where T : IResource
    {
        return innerBuilder.AddResource(resource);
    }

    public IResourceBuilder<T> CreateResourceBuilder<T>(T resource) where T : IResource
    {
        return innerBuilder.CreateResourceBuilder(resource);
    }

    public DistributedApplication Build()
    {
        builtApp = true;
        return innerBuilder.Build();
    }

    public void Dispose()
    {
        // When the builder is disposed we build a host and then dispose it.
        // This cleans up unmanaged resources on the inner builder.
        if (!builtApp)
        {
            try
            {
                innerBuilder.Build().Dispose();
            }
            catch
            {
                // Ignore errors.
            }
        }
    }
}

