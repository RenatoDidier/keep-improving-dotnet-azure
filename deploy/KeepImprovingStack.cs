using Pulumi;
using Pulumi.AzureNative.ContainerRegistry;
using Pulumi.AzureNative.ContainerRegistry.Inputs;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Web;
using Pulumi.AzureNative.Web.Inputs;
using Pulumi.Docker;
using Pulumi.Docker.Inputs;
using System;
using System.Linq;

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
    public KeepImprovingStack()
    {
        var resourceGroup = ResourceGroup.Get("rg-keepimproving-dev-brs", "/subscriptions/39a689b6-9fb4-4598-a6d7-9bd1994848ab/resourceGroups/rg-keepimproving-dev-brs");

        var projectName = PulumiNameFormatter.Format(Pulumi.Deployment.Instance.ProjectName);
        var pulumiStack = Pulumi.Deployment.Instance.StackName;

        var appServicePlan = new AppServicePlan("appServicePlan", new()
        {
            Name = $"asp-{projectName}-{pulumiStack}",
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

        var acr = new Registry("acrkeepimproving", new Pulumi.AzureNative.ContainerRegistry.RegistryArgs
        {
            ResourceGroupName = resourceGroup.Name,
            Location = resourceGroup.Location,
            Sku = new SkuArgs
            {
                Name = "Basic"
            },
            AdminUserEnabled = true
        });

        var acrCredentials = Output.Tuple(resourceGroup.Name, acr.Name).Apply(t =>
        {
            var (rg, registryName) = t;

            return ListRegistryCredentials.Invoke(new ListRegistryCredentialsInvokeArgs
            {
                ResourceGroupName = rg,
                RegistryName = registryName
            });
        });
        var acrUsername = acrCredentials.Apply(c => c.Username ?? "");
        var acrPassword = acrCredentials.Apply(c => Output.CreateSecret(c.Passwords.First().Value ?? ""));

        var image = new Image("keepimproving-image", new ImageArgs
        {
            ImageName = Output.Format($"{acr.LoginServer}/keepimproving-image:{DateTime.UtcNow.ToString("yyyyMMddhhmmss")}"),
            Build = new DockerBuildArgs
            {
                Context = "..",
                Dockerfile = "../Dockerfile",
                Platform = "linux/amd64"
            },
            Registry = new Pulumi.Docker.Inputs.RegistryArgs
            {
                Server = acr.LoginServer,
                Username = acrUsername,
                Password = acrPassword
            }
        });

        var webApp = new WebApp("webApp", new()
        {
            Kind = "app,linux",
            Location = resourceGroup.Location,
            ResourceGroupName = resourceGroup.Name,
            Name = $"app-{projectName}-{pulumiStack}-api",
            ServerFarmId = appServicePlan.Id,
            SiteConfig = new SiteConfigArgs
            {
                LinuxFxVersion = Output.Format($"DOCKER|{image.ImageName}"),
                AppSettings =
                {
                    new NameValuePairArgs
                    {
                        Name = "DOCKER_REGISTRY_SERVER_URL",
                        Value = Output.Format($"https://{acr.LoginServer}")
                    },
                    new NameValuePairArgs
                    {
                        Name = "DOCKER_REGISTRY_SERVER_USERNAME",
                        Value = acrUsername
                    },
                    new NameValuePairArgs
                    {
                        Name = "DOCKER_REGISTRY_SERVER_PASSWORD",
                        Value = acrPassword
                    },
                    new NameValuePairArgs
                    {
                        Name = "WEBSITES_PORT",
                        Value = "8080"
                    },
                    new NameValuePairArgs
                    {
                        Name = "ASPNETCORE_ENVIRONMENT",
                        Value = "Production"
                    }
                },
                AutoHealEnabled = true,
                HealthCheckPath = "/health",
            },
        });
    }
}
