using Microsoft.EntityFrameworkCore;
using VoltaMarcada.Application.Support;
using VoltaMarcada.Infrastructure.Persistence;

namespace VoltaMarcada.Infrastructure.Support;

public sealed class SupportRequestContextReader(AppDbContext dbContext) : ISupportRequestContextReader
{
    public Task<SupportRequestContext?> GetAsync(
        Guid userId,
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        (from membership in dbContext.CompanyUsers.AsNoTracking()
         join user in dbContext.Users.AsNoTracking() on membership.UserId equals user.Id
         join company in dbContext.Companies.AsNoTracking() on membership.CompanyId equals company.Id
         where membership.UserId == userId && membership.CompanyId == companyId
         select new SupportRequestContext(
             user.FullName ?? user.Email ?? string.Empty,
             user.Email ?? string.Empty,
             company.TradeName,
             company.LegalName))
        .SingleOrDefaultAsync(cancellationToken);
}
