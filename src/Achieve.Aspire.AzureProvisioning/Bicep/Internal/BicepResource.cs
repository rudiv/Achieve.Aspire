using Bicep.Core.Parsing;
using Bicep.Core.Syntax;
using Google.Protobuf.WellKnownTypes;

namespace Achieve.Aspire.AzureProvisioning.Bicep.Internal;

public abstract class BicepResource(string type) : IBicepSyntaxGenerator
{
    private const string ExistingToken = "existing";

    private string name;
    public string Name
    {
        get => name;
        set => name = ValidateResourceName(value);
    }

    private string ValidateResourceName(string name)
    {
        if (name.Any(m => !char.IsLetterOrDigit(m) && m != '_'))
        {
            throw new ArgumentException("Resource name must be alphanumeric with underscores only.");
        }

        return name;
    }

    public string Type { get; set; } = type;
    public bool Existing { get; set; } = false;

    protected List<IBicepResourceProperty> Body { get; } = [];

    protected List<BicepResource> ChildResources { get; } = [];

    protected virtual void ValidateResourceType() { }

    public abstract void Construct();

    public virtual BicepResource AsExisting() => throw new NotImplementedException("This resource does not yet support AsExisting.");
    
    public SyntaxBase ToBicepSyntax()
    {
        Body.Clear();
        ValidateResourceType();
        Construct();
        
        return new ResourceDeclarationSyntax([],
            SyntaxFactory.ResourceKeywordToken,
            SyntaxFactory.CreateIdentifierWithTrailingSpace(Name),
            SyntaxFactory.CreateStringLiteral(Type),
            Existing ? SyntaxFactory.CreateIdentifierToken(ExistingToken, SyntaxFactory.SingleSpaceTrivia) : null,
            SyntaxFactory.CreateToken(TokenType.Assignment, SyntaxFactory.SingleSpaceTrivia, SyntaxFactory.SingleSpaceTrivia),
            [],
            CreateIndentedObject(Body.Select(m => (ObjectPropertySyntax)m.ToBicepSyntax())));
    }

    public static SyntaxBase CreateIndentedObject(IEnumerable<ObjectPropertySyntax> properties, int indent = 2)
    {
        var indentStr = new string(' ', indent);
        var indentBack = new string(' ', indent - 2);
        var children = new List<SyntaxBase>();
        var propertyList = properties.ToList();
        if (propertyList.Count > 0)
        {
            children.Add(SyntaxFactory.CreateNewLineWithIndent(indentStr));
        }
        
        for(int i = 0; i < propertyList.Count; i++)
        {
            children.Add(propertyList[i]);
            if (i == propertyList.Count - 1)
            {
                children.Add(SyntaxFactory.CreateNewLineWithIndent(indentBack));
            }
            else
            {
                children.Add(SyntaxFactory.CreateNewLineWithIndent(indentStr));
            }
        }

        return new ObjectSyntax(
            SyntaxFactory.LeftBraceToken,
            children,
            SyntaxFactory.RightBraceToken);
    }
}

public interface IBicepResourceProperty : IBicepSyntaxGenerator { }

public class BicepResourceProperty(string name, BicepValue bicepValue) : IBicepResourceProperty
{
    public virtual SyntaxBase ToBicepSyntax() => SyntaxFactory.CreateObjectProperty(name, bicepValue.ToBicepSyntax());
}

public class BicepResourcePropertyBag(string name, int level = 1) : IBicepResourceProperty
{
    private List<IBicepResourceProperty> Properties { get; } = new();
    
    private bool IsRawObject { get; set; } = false;

    public BicepResourcePropertyBag AsValueOnly()
    {
        IsRawObject = true;
        return this;
    }
    
    public BicepResourcePropertyBag AddProperty(IBicepResourceProperty property)
    {
        Properties.Add(property);
        return this;
    }
    
    public BicepResourcePropertyBag AddProperty(string name, BicepValue value)
    {
        Properties.Add(new BicepResourceProperty(name, value));
        return this;
    }

    public SyntaxBase ToBicepSyntax()
    {
        var rawObject = BicepResource.CreateIndentedObject(Properties.Select(p => (ObjectPropertySyntax)p.ToBicepSyntax()), 2 + (level * 2));
        if (IsRawObject)
        {
            return rawObject;
        }
        else
        {
            return SyntaxFactory.CreateObjectProperty(name, rawObject);
        }
    }
}

public class BicepResourcePropertyArray(string name, int level = 1) : IBicepResourceProperty
{
    private List<IBicepSyntaxGenerator> Values { get; } = new();
    
    public BicepResourcePropertyArray AddValue(IBicepSyntaxGenerator value)
    {
        Values.Add(value);
        return this;
    }
    
    public SyntaxBase ToBicepSyntax() => SyntaxFactory.CreateObjectProperty(name,
        CreateIndentedArray(Values.Select(v => v.ToBicepSyntax()), 2 + (level * 2)));
    
    public static SyntaxBase CreateIndentedArray(IEnumerable<SyntaxBase> values, int indent = 2)
    {
        var indentStr = new string(' ', indent);
        var indentBack = new string(' ', indent - 2);
        var children = new List<SyntaxBase>();
        var valueList = values.ToList();
        
        if (valueList.Count != 0)
        {
            children.Add(SyntaxFactory.CreateNewLineWithIndent(indentStr));
        }

        for(int i = 0; i < valueList.Count; i++)
        {
            children.Add(SyntaxFactory.CreateArrayItem(valueList[i]));
            if (i == valueList.Count - 1)
            {
                children.Add(SyntaxFactory.CreateNewLineWithIndent(indentBack));
            }
            else
            {
                children.Add(SyntaxFactory.CreateNewLineWithIndent(indentStr));
            }
        }

        return new ArraySyntax(
            SyntaxFactory.LeftSquareToken,
            children,
            SyntaxFactory.RightSquareToken);
    }
}

public static class BicepResourceProperties
{
    public const string Name = "name";
    public const string Scope = "scope";
    public const string Location = "location";
    public const string Properties = "properties";
}