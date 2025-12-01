using System.Threading.Tasks;
using KeepImproving.Deploy;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        return await Pulumi.Deployment.RunAsync<KeepImprovingStack>();
    }
}