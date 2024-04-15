using Aspire.Hosting.Azure;
using Azure.Provisioning;
using Bicep.Core.Syntax;

namespace Achieve.Aspire.AzureProvisioning.Bicep.Internal;

public class BicepFileOutput
{
    private const string LocationParameter = "location";
    
    private List<BicepParameter> Parameters { get; } = [];
    private List<BicepResource> Resources { get; } = [];
    private List<BicepOutput> Outputs { get; } = [];
    
    private static TargetScopeSyntax rgTarget = new(
        SyntaxFactory.TargetScopeKeywordToken,
        SyntaxFactory.AssignmentToken,
        SyntaxFactory.CreateStringLiteral("resourceGroup"));
    
    public void AddParameter(BicepParameter parameter) => Parameters.Add(parameter);
    public void AddResource(BicepResource resource) => Resources.Add(resource);
    public void AddOutput(BicepOutput output) => Outputs.Add(output);

    public static BicepFileOutput GetAspireFileOutput(bool includePrincipalInformation = false)
    {
        var fileOutput = new BicepFileOutput
        {
            Parameters =
            {
                new BicepParameter(
                    "location",
                    BicepSupportedType.String,
                    new BicepPropertyAccessValue(new BicepFunctionCallValue("resourceGroup"), "location"),
                    "The location of the resource group.")
            }
        };

        if (includePrincipalInformation)
        {
            fileOutput.AddParameter(new(
                AzureBicepResource.KnownParameters.PrincipalId,
                BicepSupportedType.String,
                Description: "(Aspire Provided) Principal ID"));
            
            fileOutput.AddParameter(new(
                AzureBicepResource.KnownParameters.PrincipalType,
                BicepSupportedType.String,
                Description: "(Aspire Provided) Principal Type"));
        }

        return fileOutput;
    }
    
    
    public ProgramSyntax ToBicep()
    {
        var output = Parameters.SelectMany(m => new[] { m.ToBicepSyntax(), SyntaxFactory.NewlineToken, SyntaxFactory.NewlineToken })
            .Concat(Resources.SelectMany(m => new[] { m.ToBicepSyntax(), SyntaxFactory.NewlineToken, SyntaxFactory.NewlineToken }))
            .Concat(Outputs.SelectMany(m => new[] { m.ToBicepSyntax(), SyntaxFactory.NewlineToken }))
            .Prepend(SyntaxFactory.NewlineToken)
            .Prepend(SyntaxFactory.NewlineToken)
            .Prepend(rgTarget);
        
        return new ProgramSyntax(output, SyntaxFactory.EndOfFileToken);
    }
}