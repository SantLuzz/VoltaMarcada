using Microsoft.AspNetCore.Identity;

namespace ServiceSync.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string? FullName { get; set; }

    public string? Document { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }
}
