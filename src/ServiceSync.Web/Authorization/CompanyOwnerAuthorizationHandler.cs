using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ServiceSync.Domain.Enums;
using ServiceSync.Infrastructure.Persistence;

namespace ServiceSync.Web.Authorization;

public sealed class CompanyOwnerRequirement : IAuthorizationRequirement
{
}

public sealed class CompanyOwnerAuthorizationHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : AuthorizationHandler<CompanyOwnerRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CompanyOwnerRequirement requirement)
    {
        var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var isOwner = await dbContext.CompanyUsers.AsNoTracking().AnyAsync(
            companyUser => companyUser.UserId == userId && companyUser.Role == CompanyUserRole.Owner);
        if (isOwner)
        {
            context.Succeed(requirement);
        }
    }
}
