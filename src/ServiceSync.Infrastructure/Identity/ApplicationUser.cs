using Microsoft.AspNetCore.Identity;
using ServiceSync.Domain.Entities;

namespace ServiceSync.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public Guid? CompanyId { get; set; }

    public Company? Company { get; set; }
}
