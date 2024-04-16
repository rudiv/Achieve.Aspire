using Bicep.Core.Syntax;
using Google.Protobuf.Reflection;

namespace Achieve.Aspire.AzureProvisioning.Bicep.Internal;

public interface IBicepValue : IBicepSyntaxGenerator { }

public abstract class BicepValue : IBicepValue
{
    public abstract SyntaxBase ToBicepSyntax();
}

public class BicepStringValue(string value) : BicepValue
{
    public override SyntaxBase ToBicepSyntax() => SyntaxFactory.CreateStringLiteral(value);
}

public class BicepInterpolatedString : BicepValue
{
    private List<string> Strings { get; } = [];
    private List<BicepValue> Expressions { get; } = [];
    
    public BicepInterpolatedString Str(string str)
    {
        Strings.Add(str);
        return this;
    }

    public BicepInterpolatedString Exp(BicepValue expression)
    {
        Expressions.Add(expression);
        return this;
    }

    public override SyntaxBase ToBicepSyntax()
    {
        if (Strings.Count == 0 && Expressions.Count > 1)
        {
            // Safely default to a non-string
            return Expressions[0].ToBicepSyntax();
        } else if (Strings.Count > 1 && Expressions.Count == 0)
        {
            return SyntaxFactory.CreateStringLiteral(string.Concat(Strings));
        } else if (Strings.Count <= Expressions.Count)
        {
            // Try to save a broken scenario
            // TODO More robust
            Strings.Add(string.Empty);
        }

        return SyntaxFactory.CreateString(Strings, Expressions.Select(e => e.ToBicepSyntax()).ToArray());
    }
}

public class BicepBooleanValue(bool value) : BicepValue
{
    public override SyntaxBase ToBicepSyntax() => SyntaxFactory.CreateBooleanLiteral(value);
}

public class BicepIntValue(int value) : BicepValue
{
    public override SyntaxBase ToBicepSyntax() => SyntaxFactory.CreateIntegerLiteral((ulong)value);
}

public class BicepVariableValue(string variable) : BicepValue
{
    public override SyntaxBase ToBicepSyntax() => SyntaxFactory.CreateVariableAccess(variable);
}

public class BicepFunctionCallValue(string function, params BicepValue[] arguments) : BicepValue
{
    private List<BicepValue> Arguments { get; } = new(arguments);
    
    public BicepFunctionCallValue WithArgument(BicepValue argument)
    {
        Arguments.Add(argument);
        return this;
    }
    
    public override SyntaxBase ToBicepSyntax() => SyntaxFactory.CreateFunctionCall(function, Arguments.Select(a => a.ToBicepSyntax()).ToArray());
}

public class BicepPropertyAccessValue(BicepValue baseValue, string value) : BicepValue
{
    public override SyntaxBase ToBicepSyntax() => SyntaxFactory.CreatePropertyAccess(baseValue.ToBicepSyntax(), value);
}