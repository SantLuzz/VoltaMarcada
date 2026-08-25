using Microsoft.EntityFrameworkCore;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Infrastructure.Persistence.Repositories;

public sealed class CompanyRepository(AppDbContext dbContext) : ICompanyRepository
{
    public Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Companies.SingleOrDefaultAsync(company => company.Id == id, cancellationToken);

    public Task AddAsync(Company company, CancellationToken cancellationToken = default) =>
        dbContext.Companies.AddAsync(company, cancellationToken).AsTask();
}
