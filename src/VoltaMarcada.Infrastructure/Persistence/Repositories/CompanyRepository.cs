using Microsoft.EntityFrameworkCore;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Infrastructure.Persistence.Repositories;

public sealed class CompanyRepository(AppDbContext dbContext) : ICompanyRepository
{
    public Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Companies.Include(company => company.BusinessHours).SingleOrDefaultAsync(company => company.Id == id, cancellationToken);

    public Task<Company?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        dbContext.Companies.AsNoTracking().Include(company => company.BusinessHours).SingleOrDefaultAsync(company => company.Slug == slug, cancellationToken);

    public Task<CompanyQuotePdfProfile?> GetQuotePdfProfileAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        dbContext.Companies.AsNoTracking()
            .Where(company => company.Id == companyId)
            .Select(company => new CompanyQuotePdfProfile(
                company.Id,
                company.LegalName,
                company.TradeName,
                company.Document,
                company.WhatsApp,
                company.PostalCode,
                company.Address,
                dbContext.Cities.Where(city => city.Id == company.CityId).Select(city => city.Name).Single(),
                dbContext.States.Where(state => state.Id == company.StateId).Select(state => state.Abbreviation).Single(),
                company.LogoKey))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<CompanyQuotePdfProfile?> GetQuotePdfProfileAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        dbContext.Companies.AsNoTracking()
            .Where(company => company.Id == companyId)
            .Select(company => new CompanyQuotePdfProfile(
                company.Id,
                company.LegalName,
                company.TradeName,
                company.Document,
                company.WhatsApp,
                company.PostalCode,
                company.Address,
                dbContext.Cities.Where(city => city.Id == company.CityId).Select(city => city.Name).Single(),
                dbContext.States.Where(state => state.Id == company.StateId).Select(state => state.Abbreviation).Single(),
                company.LogoKey))
            .SingleOrDefaultAsync(cancellationToken);

    public Task AddAsync(Company company, CancellationToken cancellationToken = default) =>
        dbContext.Companies.AddAsync(company, cancellationToken).AsTask();

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
