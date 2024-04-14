using Bicep.Core.Syntax;
using Bicep.Core.Workspaces;

namespace Achieve.Aspire.AzureProvisioning.Bicep;

internal class BicepFileOutput
{   
    internal List<BicepParameter> Parameters { get; } = [];
    internal List<BicepResource> Resources { get; } = [];
    internal List<BicepOutput> Outputs { get; } = [];
    
    private static TargetScopeSyntax rgTarget = new(
        SyntaxFactory.TargetScopeKeywordToken,
        SyntaxFactory.AssignmentToken,
        SyntaxFactory.CreateStringLiteral("resourceGroup"));
    
    public ProgramSyntax ToBicep()
    {
        var statements = new List<SyntaxBase>();
        
        foreach(var parameter in Parameters)
        {
            statements.Add(parameter.ToBicepSyntax());
        }
        
        return new ProgramSyntax(statements.SelectMany(s => new [] { s, SyntaxFactory.NewlineToken }), SyntaxFactory.EndOfFileToken);
    }
}