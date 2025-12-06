using Pulumi;
using Pulumi.Docker;
using Pulumi.Docker.Inputs;
using Pulumi.AzureNative.ContainerRegistry;
using Pulumi.AzureNative.ContainerRegistry.Inputs;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Web;
using Pulumi.AzureNative.Web.Inputs;
using System;

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
        //var configDocker = new Pulumi.Config("KeepImproving");

        //var dockerHubUser = configDocker.RequireSecret("dockerHubUser");
        //var dockerHubToken = configDocker.RequireSecret("dockerHubToken");

        //var image = new Image("keepimproving-image", new ImageArgs
        //{
        //    ImageName = dockerHubUser.Apply(user =>
        //        $"docker.io/{user.ToLower()}/keepimproving-api:latest"
        //    ),
        //    Build = new DockerBuildArgs
        //    {
        //        Context = "../src/external/private/KeepImproving.API",
        //        Dockerfile = "../src/external/private/KeepImproving.API/Dockerfile",
        //        Platform = "linux/amd64"
        //    },
        //    Registry = new RegistryArgs
        //    {
        //        Server = "docker.io",
        //        Username = dockerHubUser,
        //        Password = dockerHubToken
        //    }
        //});

        var resourceGroup = ResourceGroup.Get("rg-keepimproving-dev-brs", "/subscriptions/39a689b6-9fb4-4598-a6d7-9bd1994848ab/resourceGroups/rg-keepimproving-dev-brs");

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

        var acrCredentials = ListRegistryCredentials.Invoke(new()
        {
            ResourceGroupName = resourceGroup.Name,
            RegistryName = acr.Name
        });

        var acrUsername = acrCredentials.Apply(c => c.Username);
        var acrPassword = acrCredentials.Apply(c => c.Passwords[0].Value);

        var uniqueTag = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

        var image = new Image("keepimproving-image", new ImageArgs
        {
            ImageName = acr.LoginServer.Apply(server =>
                $"{server}/keepimproving-api:{uniqueTag}"
            ),
            Build = new DockerBuildArgs
            {
                Context = "../src/external/private/KeepImproving.API",
                Dockerfile = "src/external/private/KeepImproving.API/Dockerfile",
                Platform = "linux/amd64"
            },
            Registry = new Pulumi.Docker.Inputs.RegistryArgs
            {
                Server = acr.LoginServer,
                Username = acrUsername,
                Password = acrPassword
            }
        });


        var stack = Pulumi.Deployment.Instance.StackName;
        var projectName = PulumiNameFormatter.Format(Pulumi.Deployment.Instance.ProjectName);



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
            Kind = "app,linux",
            Location = resourceGroup.Location,
            ResourceGroupName = resourceGroup.Name,
            Name = $"app-{projectName}-{stack}-api",
            ServerFarmId = appServicePlan.Id,
            SiteConfig = new SiteConfigArgs
            {
                LinuxFxVersion = image.ImageName.Apply(img => $"DOCKER|{img}"),

                AppSettings =
                {
                    new NameValuePairArgs
                    {
                        Name = "WEBSITES_PORT",
                        Value = "8080"
                    }

                },
                AutoHealEnabled = true,
                HealthCheckPath = "/health",
            }
        });
    }
}
