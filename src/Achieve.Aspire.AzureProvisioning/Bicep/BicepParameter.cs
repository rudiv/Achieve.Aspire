using Bicep.Core.Syntax;

namespace Achieve.Aspire.AzureProvisioning.Bicep;

internal record BicepParameter(string Name, BicepSupportedType Type, string? DefaultValue = default, string? Description = default) : IBicepSyntaxGenerator
{
    public SyntaxBase ToBicepSyntax() =>
        new ParameterDeclarationSyntax(GetDescriptionSyntax(),
            SyntaxFactory.ParameterKeywordToken,
            SyntaxFactory.CreateIdentifierWithTrailingSpace(Name),
            GetParameterTypeSyntax(),
            GetDefaultValueSyntax()
        );

    private IEnumerable<SyntaxBase> GetDescriptionSyntax()
    {
        if (Description == null) return [];

        return
        [
            SyntaxFactory.CreateDecorator("description", SyntaxFactory.CreateStringLiteral(Description)),
            SyntaxFactory.NewlineToken
        ];
    }

    private SyntaxBase? GetDefaultValueSyntax()
    {
        if (DefaultValue == null) return default;

        return new ParameterDefaultValueSyntax(SyntaxFactory.AssignmentToken, SyntaxFactory.CreateStringLiteral(DefaultValue));
    }

    private VariableAccessSyntax GetParameterTypeSyntax()
    {
        Func<string, IdentifierSyntax> createIdentifier =
            DefaultValue == null ? SyntaxFactory.CreateIdentifier : SyntaxFactory.CreateIdentifierWithTrailingSpace;
        return Type switch
        {
            BicepSupportedType.String => new VariableAccessSyntax(createIdentifier("string")),
            _ => throw new ArgumentOutOfRangeException(nameof(Type), Type, null)
        };
    }
}
