using Microsoft.EntityFrameworkCore;
using ServiceSync.Application.Abstractions.Identity;
using ServiceSync.Application.Abstractions.Tenancy;
using ServiceSync.Infrastructure.Persistence;

namespace ServiceSync.Web.Identity;

public sealed class CurrentCompany(AppDbContext dbContext, ICurrentUser currentUser) : ICurrentCompany
{
    private readonly Lazy<Guid> companyId = new(() => ResolveCompanyId(dbContext, currentUser));

    public Guid CompanyId => companyId.Value;

    private static Guid ResolveCompanyId(AppDbContext dbContext, ICurrentUser currentUser)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId == Guid.Empty)
        {
            throw new InvalidOperationException("A current company can only be resolved for an authenticated user.");
        }

        return dbContext.CompanyUsers
            .AsNoTracking()
            .Where(companyUser => companyUser.UserId == currentUser.UserId)
            .Select(companyUser => companyUser.CompanyId)
            .Single();
    }
}
