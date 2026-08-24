using Microsoft.EntityFrameworkCore;
using ServiceSync.Application.Abstractions.Identity;
using ServiceSync.Application.Abstractions.Tenancy;
using ServiceSync.Infrastructure.Persistence;

namespace ServiceSync.Web.Identity;

public sealed class CurrentCompany(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ICurrentUser currentUser) : ICurrentCompany
{
    public async Task<Guid?> FindCompanyIdAsync(CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId == Guid.Empty)
        {
            return null;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var companyId = await dbContext.CompanyUsers
            .AsNoTracking()
            .Where(companyUser => companyUser.UserId == currentUser.UserId)
            .Select(companyUser => companyUser.CompanyId)
            .SingleOrDefaultAsync(cancellationToken);

        return companyId == Guid.Empty ? null : companyId;
    }

    public async Task<Guid> GetCompanyIdAsync(CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId == Guid.Empty)
        {
            throw new InvalidOperationException("A current company can only be resolved for an authenticated user.");
        }

        return await FindCompanyIdAsync(cancellationToken) ?? throw new CurrentCompanyNotFoundException();
    }
}
