using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using VoltaMarcada.Infrastructure.Identity;
using VoltaMarcada.Infrastructure.Persistence;

namespace VoltaMarcada.Web.Authorization;

public sealed class CompanyMemberRequirement : IAuthorizationRequirement
{
}

public sealed class CompanyMemberAuthorizationHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : AuthorizationHandler<CompanyMemberRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CompanyMemberRequirement requirement)
    {
        if (context.User.IsInRole(PlatformRoles.PlatformAdministrator))
        {
            context.Succeed(requirement);
            return;
        }

        var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var hasMembership = await dbContext.CompanyUsers
            .AsNoTracking()
            .AnyAsync(companyUser => companyUser.UserId == userId);

        if (hasMembership)
        {
            context.Succeed(requirement);
        }
    }
}
