using Achieve.Aspire.AzureProvisioning.Bicep.Internal;
using Aspire.Hosting.Azure;

namespace Achieve.Aspire.AzureProvisioning.Resources;

public class AchieveResource(string name, BicepFileOutput fileOutput) : AzureBicepResource(name, templateFile: name + ".achieve.bicep")
{
    /// <inheritdoc />
    public override BicepTemplateFile GetBicepTemplateFile(string? directory = null, bool deleteTemporaryFileOnDispose = true)
    {
        var generationPath = Directory.CreateTempSubdirectory("aspire").FullName;
        var output = fileOutput.ToBicep().ToString();

        var moduleDestinationPath = Path.Combine(directory ?? generationPath, $"{Name}.achieve.bicep");

        File.WriteAllText(moduleDestinationPath, output);

        return new BicepTemplateFile(moduleDestinationPath, directory is null);
    }

    private string? generatedBicep;

    /// <inheritdoc />
    public override string GetBicepTemplateString()
    {
        if (generatedBicep is null)
        {
            var template = GetBicepTemplateFile();
            generatedBicep = File.ReadAllText(template.Path);
        }

        return generatedBicep;
    }
}