using ServiceSync.Domain.Abstractions.Tenancy;

namespace ServiceSync.Application.Abstractions.Tenancy;

public static class CompanyScopedQueryableExtensions
{
    public static IQueryable<TEntity> ForCompany<TEntity>(
        this IQueryable<TEntity> source,
        Guid companyId)
        where TEntity : class, ICompanyScopedEntity
    {
        ArgumentNullException.ThrowIfNull(source);

        if (companyId == Guid.Empty)
        {
            throw new ArgumentException("Company ID is required.", nameof(companyId));
        }

        return source.Where(entity => entity.CompanyId == companyId);
    }
}
