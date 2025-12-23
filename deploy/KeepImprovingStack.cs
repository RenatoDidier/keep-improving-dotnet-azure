using Pulumi;
using Pulumi.AzureNative.ContainerRegistry;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Web;
using Pulumi.Docker;

namespace KeepImproving.Deploy;


public class KeepImprovingStack : Stack
{
    public KeepImprovingStack()
    {
        var resourceGroup = ResourceGroup.Get("rg-keepimproving-dev-brs", "/subscriptions/39a689b6-9fb4-4598-a6d7-9bd1994848ab/resourceGroups/rg-keepimproving-dev-brs");

        var projectName = Pulumi.Deployment.Instance.ProjectName;
        var pulumiStack = Pulumi.Deployment.Instance.StackName;

        ResourceFactory resourceManager = new(projectName, pulumiStack, resourceGroup);

        AppServicePlan appServicePlan = resourceManager.CreateAppServicePlan();

        Output<string> connectionString = resourceManager.CreateSqlServerAndDatabaseAndFirewall();

        (Registry acr, Output<string> acrUsername, Output<string> acrPassword) = resourceManager.CreateACRCredentials();

        Image image = resourceManager.CreateImageDocker(acr, acrUsername, acrPassword);

        resourceManager.CreateWebApp(appServicePlan, image, acr, acrUsername, acrPassword, connectionString);

    }
}
