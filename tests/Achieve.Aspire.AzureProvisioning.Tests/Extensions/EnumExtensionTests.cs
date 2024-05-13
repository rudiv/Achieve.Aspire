

using System.Runtime.Serialization;
using Achieve.Aspire.AzureProvisioning.Extensions;

namespace Achieve.Aspire.AzureProvisioning.Tests.Extensions;

public class EnumExtensionTests
{
    [Fact]
    public void CanReadValueFromEnumWithoutEnumMemberAttribute()
    {
        var sut = Shapes.Circle.GetValueFromEnumMember();
        Assert.Equal("Circle", sut);
    }

    [Fact]
    public void CanReadValueFromEnumWithEnumMemberAttribute()
    {
        var sut = Colors.Blue.GetValueFromEnumMember();
        Assert.Equal("azul", sut);
    }
    
    public enum Shapes
    {
        Circle,
        Triangle
    }
        
    private enum Colors
    {
        [EnumMember(Value="verde")]
        Green,
        [EnumMember(Value="azul")]
        Blue
    }
}
