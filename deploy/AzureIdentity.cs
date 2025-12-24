using Pulumi;

namespace KeepImproving;

public class AzureIdentity
{
    public Output<string> SubscriptionId { get; set; } = null!;
    public Output<string> TenantId { get; set; } = null!;
}
