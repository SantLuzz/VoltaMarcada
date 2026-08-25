using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Application.Abstractions.Persistence;

public interface ICompanyRepository
{
    Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Company company, CancellationToken cancellationToken = default);
}
