using Bicep.Core.Syntax;

namespace Achieve.Aspire.AzureProvisioning.Bicep;

internal interface IBicepSyntaxGenerator
{
    SyntaxBase ToBicepSyntax();
}