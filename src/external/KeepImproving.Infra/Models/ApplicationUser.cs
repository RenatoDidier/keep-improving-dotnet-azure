using Microsoft.AspNetCore.Identity;

namespace KeepImproving.Infra.Models;
public class ApplicationUser : IdentityUser
{
    public string Name { get; set; } = string.Empty;
}
