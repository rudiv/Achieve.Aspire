using Achieve.Aspire.AzureProvisioning.Bicep;
using Azure.Provisioning.Authorization;

namespace Achieve.Aspire.AzureProvisioning.RoleAssignment;

public class RoleAssignmentBicep
{
    private readonly string targetResource;
    private readonly RoleDefinition roleDefinition;
    private readonly string principalId;

    private BicepFileOutput output;

    public RoleAssignmentBicep(string targetResource, RoleDefinition roleDefinition, string principalId)
    {
        this.targetResource = targetResource;
        this.roleDefinition = roleDefinition;
        this.principalId = principalId;

        CreateOutput();
    }

    private void CreateOutput()
    {
        output = new BicepFileOutput();
        
        output.Parameters.Add(new BicepParameter());
    }

    public string GetBicepOutput() => output.ToBicep().ToString();
}