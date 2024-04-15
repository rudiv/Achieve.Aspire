using Achieve.Aspire.AzureProvisioning.Bicep;
using Achieve.Aspire.AzureProvisioning.Bicep.Internal;
using Xunit.Abstractions;

namespace Achieve.Aspire.AzureProvisioning.Tests.Bicep;

public class BasicGeneratorTests(ITestOutputHelper output)
{
    [Fact]
    public void ParametersGenerateCorrectSyntax()
    {
        var param = new BicepParameter("testParam", BicepSupportedType.String);
        Assert.Equal("param testParam string", param.ToBicepSyntax().ToString());

        param = new BicepParameter("testParam", BicepSupportedType.String, new BicepStringValue("testValue"));
        Assert.Equal("param testParam string = 'testValue'", param.ToBicepSyntax().ToString());

        param = new BicepParameter("testParam", BicepSupportedType.String, Description: "Test Description");
        Assert.Equal("""
                     @description('Test Description')
                     param testParam string
                     """, param.ToBicepSyntax().ToString());
        
        param = new BicepParameter("testParam", BicepSupportedType.String, new BicepStringValue("testValue"), "Test Description");
        Assert.Equal("""
                     @description('Test Description')
                     param testParam string = 'testValue'
                     """, param.ToBicepSyntax().ToString());
    }

    [Fact]
    public void OutputsGenerateCorrectSyntax()
    {
        var output = new BicepOutput("testOutput", BicepSupportedType.String, "resourceName.id");
        Assert.Equal("output testOutput string = resourceName.id", output.ToBicepSyntax().ToString());
        output = new BicepOutput("testOutput", BicepSupportedType.String, "resourceName.properties.anotherId");
        Assert.Equal("output testOutput string = resourceName.properties.anotherId", output.ToBicepSyntax().ToString());
    }

    [Fact]
    public void ResourceGeneratesCorrectHeader()
    {
        var emptyResource = new EmptyResource("test");
        Assert.Equal("resource test 'Test/test@2023-01-01' = {\n}", emptyResource.ToBicepSyntax().ToString());

        emptyResource.Existing = true;
        Assert.Equal("resource test 'Test/test@2023-01-01' existing = {\n}", emptyResource.ToBicepSyntax().ToString());
    }

    [Fact]
    public void ValueGenerationCorrect()
    {
        var stringValue = new BicepStringValue("test");
        Assert.Equal("'test'", stringValue.ToBicepSyntax().ToString());
        
        var intValue = new BicepIntValue(42);
        Assert.Equal("42", intValue.ToBicepSyntax().ToString());

        var functionCallValue = new BicepFunctionCallValue("test", new BicepStringValue("test1"));
        Assert.Equal("test('test1')", functionCallValue.ToBicepSyntax().ToString());

        functionCallValue = new BicepFunctionCallValue("test",
            new BicepFunctionCallValue("test1", new BicepStringValue("test2"),
                new BicepVariableValue("test3")),
            new BicepStringValue("test4"));
        Assert.Equal("test(test1('test2',test3),'test4')", functionCallValue.ToBicepSyntax().ToString());
        
        var propertyAccessValue = new BicepPropertyAccessValue(new BicepVariableValue("test"), "test1");
        Assert.Equal("test.test1", propertyAccessValue.ToBicepSyntax().ToString());

        var interpolatedStringValue = new BicepInterpolatedString()
            .Str("Hello")
            .Exp(new BicepFunctionCallValue("world"));
        Assert.Equal("'Hello${world()}'", interpolatedStringValue.ToBicepSyntax().ToString());
    }

    private class EmptyResource : BicepResource
    {
        public EmptyResource(string name) : base("Test/test@2023-01-01")
        {
            Name = name;
        }
        
        public override void Construct()
        {
            
        }
    }

    private class PropertyResource : BicepResource
    {
        public PropertyResource(string name) : base("Test/test@2023-01-01")
        {
            Name = name;
        }
        public override void Construct()
        {
            Body.Add(new BicepResourceProperty("testProp", new BicepStringValue("testValue")));
        }
    }
}