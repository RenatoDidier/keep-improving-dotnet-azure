using KeepImproving;
using Pulumi;
using Pulumi.AzureNative.Authorization;
using Pulumi.AzureNative.ContainerRegistry;
using Pulumi.AzureNative.ContainerRegistry.Inputs;
using Pulumi.AzureNative.KeyVault;
using Pulumi.AzureNative.ManagedIdentity;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Web;
using Pulumi.AzureNative.Web.Inputs;
using Pulumi.Docker;
using Pulumi.Docker.Inputs;
using AzureNative = Pulumi.AzureNative;

public class ResourceFactory
{
    private readonly string _projectName;
    private readonly string _pulumiStack;
    private readonly ResourceGroup _resourceGroup;
    private readonly AzureIdentity _azureIdentity;

    private readonly Pulumi.Config _pulumiSecrets;
    private readonly InputMap<string> _tags;


    public ResourceFactory(
            string projectName, 
            string pulumiStack, 
            ResourceGroup resourceGroup,
            AzureIdentity azureIdentity
        )
    {
        _projectName = PulumiNameFormatter.Format(projectName);
        _pulumiStack = pulumiStack;
        _resourceGroup = resourceGroup;
        _azureIdentity = azureIdentity;

        _pulumiSecrets = new Pulumi.Config(projectName);
        _tags = new InputMap<string>()
        {
            {"Project", projectName },
            {"Stack", pulumiStack}
        };

    }

    public AppServicePlan CreateAppServicePlan()
    {
        AppServicePlan appServicePlan = new ("appServicePlan", new()
        {
            Name = $"asp-{_projectName}-{_pulumiStack}",
            Kind = "linux",
            Tags = _tags,
            Location = _resourceGroup.Location,
            ResourceGroupName = _resourceGroup.Name,
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

        return appServicePlan;
    }

    public Vault CreateKeyVault()
    {

        Output<string> tenantId = _azureIdentity.TenantId.Apply(tenantId => tenantId);

        Vault keyVault = new($"kv-{_pulumiStack}", new VaultArgs
        {
            ResourceGroupName = _resourceGroup.Name,
            Tags = _tags!,
            Location = _resourceGroup.Location,
            Properties = new AzureNative.KeyVault.Inputs.VaultPropertiesArgs
            {
                TenantId = tenantId,
                Sku = new AzureNative.KeyVault.Inputs.SkuArgs
                {
                    Name = AzureNative.KeyVault.SkuName.Standard,
                    Family = AzureNative.KeyVault.SkuFamily.A,
                },
                PublicNetworkAccess = "Enabled",
                EnableRbacAuthorization = true,
                EnabledForDeployment = true,
                EnabledForDiskEncryption = true,
                EnabledForTemplateDeployment = true,
            },
        });

        return keyVault;
    }

    public UserAssignedIdentity CreateUserAssignedIdentity()
    {
        var identity = new UserAssignedIdentity($"uami-{_projectName}-{_pulumiStack}", new()
        {
            ResourceGroupName = _resourceGroup.Name,
            Location = _resourceGroup.Location,
            Tags = _tags!
        });

        return identity;
    }

    public void AssignKeyVaultAccessThroughIdentity(Vault keyVault, UserAssignedIdentity userIdentity)
    {
        Output<string> roleDefinitionId = _azureIdentity.SubscriptionId.Apply(subId => $"/subscriptions/{subId}/providers/Microsoft.Authorization/roleDefinitions/4633458b-17de-408a-b874-0445c86b69e6");

        new RoleAssignment($"rbac-kv-{_projectName}-{_pulumiStack}", new()
        {
            Scope = keyVault.Id,
            RoleDefinitionId = roleDefinitionId,
            PrincipalId = userIdentity.PrincipalId,
            PrincipalType = PrincipalType.ServicePrincipal
        });
    }

    public (Registry acr, Output<string> acrUsername, Output<string> acrPassword) CreateACRCredentials()
    {
        Registry acr = new ("acrkeepimproving", new Pulumi.AzureNative.ContainerRegistry.RegistryArgs
        {
            ResourceGroupName = _resourceGroup.Name,
            Location = _resourceGroup.Location,
            Sku = new SkuArgs
            {
                Name = "Basic"
            },
            AdminUserEnabled = true
        });

        Output<ListRegistryCredentialsResult>? acrCredentials = Output.Tuple(_resourceGroup.Name, acr.Name).Apply(t =>
        {
            (string? rg, string? registryName) = t;

            return ListRegistryCredentials.Invoke(new ListRegistryCredentialsInvokeArgs
            {
                ResourceGroupName = rg,
                RegistryName = registryName
            });
        });
        Output<string> acrUsername = acrCredentials.Apply(c => c.Username ?? "");
        Output<string> acrPassword = acrCredentials.Apply(c => Output.CreateSecret(c.Passwords.First().Value ?? ""));


        return (acr, acrUsername, acrPassword);
    }

    public Image CreateImageDocker(Registry acr, Output<string> acrUsername, Output<string> acrPassword)
    {
        Image image = new ("keepimproving-image", new ImageArgs
        {
            ImageName = Output.Format($"{acr.LoginServer}/keepimproving-api:latest"),
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

        return image;
    }

    public Output<string> CreateSqlServerAndDatabaseAndFirewall()
    {
        string dbUsername = _pulumiSecrets.Require("dbUsername");
        Output<string> dbPassword = _pulumiSecrets.RequireSecret("dbPassword");

        AzureNative.Sql.Server sqlServer = new("sqlServer", new()
        {
            ResourceGroupName = _resourceGroup.Name,
            Tags = _tags,
            Location = _resourceGroup.Location,
            ServerName = $"sql-{_projectName}-{_pulumiStack}",
            AdministratorLogin = dbUsername,
            AdministratorLoginPassword = dbPassword,
            Version = "12.0",
        });

        AzureNative.Sql.Database database = new("sqlDatabase", new()
        {
            ResourceGroupName = _resourceGroup.Name,
            Tags = _tags,
            ServerName = sqlServer.Name,
            DatabaseName = "keep-improving-db"
        });

        new AzureNative.Sql.FirewallRule("allow-azure-services", new()
        {
            ResourceGroupName = _resourceGroup.Name,
            ServerName = sqlServer.Name,
            FirewallRuleName = "AllowAzureServices",
            StartIpAddress = "0.0.0.0",
            EndIpAddress = "0.0.0.0"
        });

        Output<string> connectionString = Output.Tuple<string, string, string, string>(
                sqlServer.Name,
                database.Name,
                dbUsername,
                dbPassword
            )
            .Apply(values =>
            {
                (string _sqlServer, string _sqlDatabase, string _sqlUser, string _sqlPassword) = values;

                return $"Server=tcp:{_sqlServer}.database.windows.net,1433;" +
                       $"Initial Catalog={_sqlDatabase};" +
                       $"User ID={_sqlUser};" +
                       $"Password={_sqlPassword};" +
                       "Encrypt=True;TrustServerCertificate=False;";
            });


        return Output.CreateSecret(connectionString);
    }

    public void CreateWebApp(AppServicePlan appServicePlan, Image image, Registry acr, Output<string> acrUsername, Output<string> acrPassword, Output<string> connectionString)
    {
        var webApp = new WebApp("webApp", new()
        {
            Kind = "app,linux",
            Tags = _tags,
            Location = _resourceGroup.Location,
            ResourceGroupName = _resourceGroup.Name,
            Name = $"app-{_projectName}-{_pulumiStack}-api",
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
                        Name = "DOCKER_ENABLE_CI",
                        Value = "true"
                    },
                    new NameValuePairArgs
                    {
                        Name = "WEBSITES_PORT",
                        Value = "8080"
                    },
                    new NameValuePairArgs
                    {
                        Name = "ASPNETCORE_ENVIRONMENT",
                        Value = _pulumiStack
                    }
                },
                ConnectionStrings =
                {
                    new ConnStringInfoArgs
                    {
                        Name = "DefaultConnection",
                        Type = ConnectionStringType.SQLAzure,
                        ConnectionString = connectionString
                    }
                },
                AutoHealEnabled = true,
                HealthCheckPath = "/health",
            },



        });
    }
    


    public static class PulumiNameFormatter
    {
        public static string Format(string name)
        {
            return name.ToLower().Replace(".deploy", string.Empty).Replace('.', '-');
        }
    }
}

