# Why Bicep Generation?

Azure.Provisioning is restrictive in its current state.

Achieve generates its own Bicep for unsupported, or partially supported resources. It does this by relying on 
[Bicep.Core](https://github.com/Azure/bicep) to strongly define the resources, rather than just relying on string
building (which is what Azure.Provisioning does).

It's also flexible insofar as you are able to write your own Resources to be managed through Achieve by simply inheriting
from BicepResource. This is great for when something isn't supported directly but you still want C# controlled output.

## Creating your own Resources (Advanced)

Each Resource requires the ability to "Construct" itself in Bicep, with an optional method to validate the configuration
of the resource too.

If you're fluent in Bicep, you'll know that each resource requires both a resource type and a name at the very minimum,
and can also be used to obtain references to existing resources within your infrastructure.

A simple (completely fake) resource can be defined as simply as below with 1 property:

```csharp
public class TestResource : BicepResource
{
    public string TestValue { get; set; }
    public PropertyResource(string name) : base("Test/test@2023-01-01")
    {
        Name = name;
    }
    
    public override void Construct()
    {
        Body.Add(new BicepResourceProperty("test", new BicepStringValue(TestValue)));
    }
}
```

This will generate the following Bicep:

```
resource test 'Test/test@2023-01-01' = {
  test: 'Value of TestValue property'
}
```