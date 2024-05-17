using System.DirectoryServices;
using Bicep.Core.Syntax;

namespace Achieve.Aspire.AzureProvisioning.Bicep.Internal;

public record BicepOutput(string Name, BicepSupportedType Type, string Path) : IBicepSyntaxGenerator
{
    public SyntaxBase ToBicepSyntax() => new OutputDeclarationSyntax(
        [],
        SyntaxFactory.OutputKeywordToken,
        SyntaxFactory.CreateIdentifierWithTrailingSpace(Name),
        GetOutputTypeSyntax(),
        SyntaxFactory.AssignmentToken,
        GetOutputPathSyntax());
    
    private VariableAccessSyntax GetOutputTypeSyntax()
    {
        return Type switch
        {
            BicepSupportedType.String => new VariableAccessSyntax(SyntaxFactory.CreateIdentifierWithTrailingSpace("string")),
            _ => throw new ArgumentOutOfRangeException(nameof(Type), Type, null)
        };
    }

    private PropertyAccessSyntax GetOutputPathSyntax()
    {
        // This generates depending on the output path, super hacky for now
        // There will be a better way, but for now...
        var splitPath = Path.Split('.');
        var first = splitPath[0];
        var second = splitPath.Length > 1 ? splitPath[1] : splitPath[0];
        
        var propertyAccess =
            new PropertyAccessSyntax(
                new VariableAccessSyntax(SyntaxFactory.CreateIdentifier(first)),
                SyntaxFactory.DotToken,
                null,
                SyntaxFactory.CreateIdentifier(second));
        if (splitPath.Length > 2)
        {
            for (var i = 2; i < splitPath.Length; i++)
            {
                propertyAccess = new PropertyAccessSyntax(
                    propertyAccess,
                    SyntaxFactory.DotToken,
                    null,
                    SyntaxFactory.CreateIdentifier(splitPath[i]));
            }
        }

        return propertyAccess;
    }
}