using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Application.Abstractions.Persistence;

public interface IQuoteRepository
{
    Task<IQuoteTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Quote quote, CancellationToken cancellationToken = default);
    Task<Quote?> GetForCompanyAsync(Guid quoteId, Guid companyId, CancellationToken cancellationToken = default);
    Task<Quote?> GetForCompanyForUpdateAsync(Guid quoteId, Guid companyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Quote>> GetAllForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<Quote?> GetForPublicTokenAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<Quote?> GetForPublicTokenForUpdateAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IQuoteTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}
