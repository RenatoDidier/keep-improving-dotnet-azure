using Pulumi;
using Pulumi.AzureNative.DBforMySQL;
using Pulumi.AzureNative.DBforMySQL.Inputs;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Web;
using Pulumi.AzureNative.Web.Inputs;

namespace KeepImproving.Deploy;

public static class PulumiNameFormatter
{
    public static string Format(string name)
    {
        return name.ToLower().Replace(".deploy", string.Empty).Replace('.', '-');
    }
}
public class KeepImprovingStack : Stack
{
    public KeepImprovingStack() {

        var stack = Pulumi.Deployment.Instance.StackName;
        var projectName = PulumiNameFormatter.Format(Pulumi.Deployment.Instance.ProjectName);

        var resourceGroup = ResourceGroup.Get("rg-keepimproving-dev-brs", "/subscriptions/39a689b6-9fb4-4598-a6d7-9bd1994848ab/resourceGroups/rg-keepimproving-dev-brs");

        var appServicePlan = new AppServicePlan("appServicePlan", new()
        {
            Name = $"asp-{projectName}-{stack}",
            Kind = "linux",
            Location = resourceGroup.Location,
            ResourceGroupName = resourceGroup.Name,
            Sku = new SkuDescriptionArgs
            {
                Capacity = 1,
                Family = "B",
                Name = "B1",
                Size = "B1",
                Tier = "Basic",
            },
            Reserved = true
        });

        var webApp = new WebApp("webApp", new()
        {
            Kind = "linux,app",
            Location = resourceGroup.Location,
            ResourceGroupName = resourceGroup.Name,
            Name = $"app-{projectName}-{stack}-api",
            ServerFarmId = appServicePlan.Id,
            SiteConfig = new SiteConfigArgs
            {
                AutoHealEnabled = true,
                HealthCheckPath = "/health",
            }
        });
    }
}
