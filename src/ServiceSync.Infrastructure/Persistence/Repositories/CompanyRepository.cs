using Microsoft.EntityFrameworkCore;
using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Domain.Entities;

namespace ServiceSync.Infrastructure.Persistence.Repositories;

public sealed class CompanyRepository(AppDbContext dbContext) : ICompanyRepository
{
    public Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Companies.SingleOrDefaultAsync(company => company.Id == id, cancellationToken);

    public Task AddAsync(Company company, CancellationToken cancellationToken = default) =>
        dbContext.Companies.AddAsync(company, cancellationToken).AsTask();
}
