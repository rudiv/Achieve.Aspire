using Bicep.Core.Syntax;

namespace Achieve.Aspire.AzureProvisioning.Bicep.Internal;

public interface IBicepSyntaxGenerator
{
    SyntaxBase ToBicepSyntax();
}