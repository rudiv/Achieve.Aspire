using Achieve.Aspire.AzureProvisioning.Bicep;
using Xunit.Abstractions;

namespace Achieve.Aspire.AzureProvisioning.Tests.Bicep;

public class BasicGeneratorTests(ITestOutputHelper output)
{
    [Fact]
    public void ParametersGenerateCorrectSyntax()
    {
        var param = new BicepParameter("testParam", BicepSupportedType.String);
        Assert.Equal("param testParam string", param.ToBicepSyntax().ToString());

        param = new BicepParameter("testParam", BicepSupportedType.String, "testValue");
        Assert.Equal("param testParam string = 'testValue'", param.ToBicepSyntax().ToString());

        param = new BicepParameter("testParam", BicepSupportedType.String, Description: "Test Description");
        Assert.Equal("""
                     @description('Test Description')
                     param testParam string
                     """, param.ToBicepSyntax().ToString());
        
        param = new BicepParameter("testParam", BicepSupportedType.String, "testValue", "Test Description");
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
}