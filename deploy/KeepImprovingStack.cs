using Pulumi;
using Pulumi.AzureNative.Authorization;
using Pulumi.AzureNative.ContainerRegistry;
using Pulumi.AzureNative.KeyVault;
using Pulumi.AzureNative.ManagedIdentity;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Web;
using Pulumi.Docker;


namespace KeepImproving.Deploy;


public class KeepImprovingStack : Stack
{
    public KeepImprovingStack()
    {
        Output<GetClientConfigResult>? getCliente = Output.Create(GetClientConfig.InvokeAsync());
        AzureIdentity azureIdentity = new()
        {
            SubscriptionId = getCliente.Apply(c => c.SubscriptionId),
            TenantId = getCliente.Apply(c => c.TenantId),
        };
        var projectName = Pulumi.Deployment.Instance.ProjectName;
        var pulumiStack = Pulumi.Deployment.Instance.StackName;

        Output<string> resourceId = azureIdentity.SubscriptionId.Apply(subId =>
            $"/subscriptions/{subId}/resourceGroups/rg-keepimproving-dev-brs"
        );
        var resourceGroup = ResourceGroup.Get("rg-keepimproving-dev-brs", resourceId);

        ResourceFactory resourceFactory = new(projectName, pulumiStack, resourceGroup, azureIdentity);

        AppServicePlan appServicePlan = resourceFactory.CreateAppServicePlan();

        Vault keyVault = resourceFactory.CreateKeyVault();

        UserAssignedIdentity userIdentity = resourceFactory.CreateUserAssignedIdentity();

        resourceFactory.AssignKeyVaultAccessThroughIdentity(keyVault, userIdentity);

        (Registry acr, Output<string> acrUsername, Output<string> acrPassword) = resourceFactory.CreateACRCredentials();

        Image image = resourceFactory.CreateImageDocker(acr, acrUsername, acrPassword);

        Output<string> connectionString = resourceFactory.CreateSqlServerAndDatabaseAndFirewall();

        resourceFactory.CreateWebApp(appServicePlan, image, acr, acrUsername, acrPassword, connectionString);

    }
}
