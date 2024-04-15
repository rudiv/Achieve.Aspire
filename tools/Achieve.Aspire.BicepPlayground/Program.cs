// Evil, if anyone wants to help auto generate stuff, help :)

using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Bicep.Types.Az;
using Bicep.Core.Parsing;
using Bicep.Core.Semantics;
using Bicep.Core.Syntax;
using Bicep.Core.TypeSystem;
using Bicep.Core.TypeSystem.Providers;
using Bicep.Core.TypeSystem.Providers.Az;
using Bicep.Core.TypeSystem.Types;

var bicep = """
targetScope = 'resourceGroup'

@description('')
param location string = resourceGroup().location

@description('')
param connectid string


resource keyVault_wv66C4HPm 'Microsoft.KeyVault/vaults@2022-07-01' existing = {
  name: toLower(take('kv${uniqueString(resourceGroup().id)}', 24))
  location: location
}

resource roleAssignment_rKFZmaN1h 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault_wv66C4HPm
  name: guid(keyVault_wv66C4HPm.id, 'abd4420d-bdcd-fe22-91c4-4096a7f0453b', subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6'))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalId: connectid
    principalType: 'ServicePrincipal'
  }
}
""";

var parser = new Parser(bicep);
var program = parser.Program();

foreach (var decl in program.Declarations)
{
  Console.WriteLine(decl.GetType().Name);

  if (decl is ResourceDeclarationSyntax rd)
  {
    Console.WriteLine(rd.Name);
    Console.WriteLine(JsonSerializer.Serialize(rd));
  }
}