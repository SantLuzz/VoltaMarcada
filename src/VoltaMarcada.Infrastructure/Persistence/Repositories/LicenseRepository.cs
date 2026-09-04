using Microsoft.EntityFrameworkCore;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Infrastructure.Persistence.Repositories;

public sealed class LicenseRepository(AppDbContext dbContext) : ILicenseRepository
{
    public Task<CompanyLicenseMembership?> GetMembershipAndLicenseAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        (from membership in dbContext.CompanyUsers.AsNoTracking()
         join license in dbContext.CompanyLicenses.AsNoTracking()
             on membership.CompanyId equals license.CompanyId
         where membership.UserId == userId
         select new CompanyLicenseMembership(membership.Role, license))
        .SingleOrDefaultAsync(cancellationToken);

    public Task<CompanyLicense?> GetForCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        dbContext.CompanyLicenses
            .AsNoTracking()
            .SingleOrDefaultAsync(license => license.CompanyId == companyId, cancellationToken);
}
