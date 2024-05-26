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

Validation is as simple as overriding the validation method and throwing an exception when invalid.

```csharp
protected override void ValidateResourceType()
{
    if (MyProp == null)
    {
        throw new InvalidOperationException("Oops, you need to set MyProp!");
    }
}
```

For more advanced property types such as objects and arrays, there are associated methods to assist with the generation
of something like the following:

```
resource test 'Test/test@2023-01-01' = {
  test: 'Value of TestValue property'
  arr: [
    'Value 1',
    'Value 2'
  ]
  properties: {
    something: 'test'
  }
}
```

To support indentation of the resulting Bicep, you need to maintain the "level" at which you're at (2nd example below has
levels 1 and 2).

```csharp
var arrArray = new BicepResourcePropertyArray("arr", 1);
arrArray.AddValue(new BicepStringValue("Value 1"));
arrArray.AddValue(new BicepStringValue("Value 2"));
Body.Add(arrArray);

var propBag = new BicepResourcePropertyBag(BicepResourceProperties.Properties, 1);
propBag.AddProperty("something", new BicepStringValue("test"));
Body.Add(propBag);
```

You can of course nest these:

```csharp
var propBag = new BicepResourcePropertyBag(BicepResourceProperties.Properties, 1);
var newBag = new BicepResourcePropertyBag("new", 2);
newBag.AddProperty("something", new BicepStringValue("test"));
propBag.AddProperty(newBag);
Body.Add(propBag);
```

Which outputs:

```
properties: {
  new: {
    something: 'test'
  }
}
```

A Property Bag can also output without the prefix (a kindof abuse, it should be a new type, but for now...) for example
when adding it to an array.

```csharp
var array = new BicepResourcePropertyArray("arr", 1);
var valueOnlyBag = new BicepResourcePropertyBag("x", 2)
    .AsValueOnly()
    .AddProperty("something", new BicepStringValue("test"));
array.AddValue(valueOnlyBag);
```

Which outputs:

```
arr: [
  {
    something: 'test'
  }
]
```

## "Existing" Resource Support

Currently, Role Assignments require an "Existing" resource to be defined in the same Bicep. See #4 for a potential 
alternative approach.

To implement Existing support (so Role Assignments can work), override AsExisting. This is very basic, though not 
automatic until #3 is solved to standardise the Account.

```csharp
    // where AccountName is the actual "name" property in the Bicep
    public override BicepResource AsExisting() => new ExistingBicepResource(resourceType, AccountName);
```