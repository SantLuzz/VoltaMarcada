using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Infrastructure.Persistence.Repositories;

public sealed class QuoteRepository(AppDbContext dbContext) : IQuoteRepository
{
    public async Task<IQuoteTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        new QuoteTransaction(await dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken));
    public Task AddAsync(Quote quote, CancellationToken cancellationToken = default) => dbContext.Quotes.AddAsync(quote, cancellationToken).AsTask();
    public Task<Quote?> GetForCompanyAsync(Guid quoteId, Guid companyId, CancellationToken cancellationToken = default) =>
        dbContext.Quotes.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == quoteId && x.CompanyId == companyId, cancellationToken);
    public Task<Quote?> GetForCompanyForUpdateAsync(Guid quoteId, Guid companyId, CancellationToken cancellationToken = default) =>
        dbContext.Quotes.FromSqlInterpolated($"SELECT * FROM \"Quotes\" WHERE \"Id\" = {quoteId} AND \"CompanyId\" = {companyId} FOR UPDATE").Include(x => x.Items).SingleOrDefaultAsync(cancellationToken);
    public async Task<IReadOnlyList<Quote>> GetAllForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        await dbContext.Quotes.AsNoTracking().Include(x => x.Items).Where(x => x.CompanyId == companyId).OrderByDescending(x => x.CreatedAtUtc).ToArrayAsync(cancellationToken);
    public Task<Quote?> GetForPublicTokenAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        dbContext.Quotes.Include(x => x.Items).SingleOrDefaultAsync(x => x.PublicTokenHash == tokenHash && x.PublicTokenRevokedAtUtc == null, cancellationToken);
    public Task<Quote?> GetForPublicTokenForUpdateAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        dbContext.Quotes.FromSqlInterpolated($"SELECT * FROM \"Quotes\" WHERE \"PublicTokenHash\" = {tokenHash} AND \"PublicTokenRevokedAtUtc\" IS NULL FOR UPDATE").Include(x => x.Items).SingleOrDefaultAsync(cancellationToken);
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => dbContext.SaveChangesAsync(cancellationToken);

    private sealed class QuoteTransaction(IDbContextTransaction transaction) : IQuoteTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default) => transaction.CommitAsync(cancellationToken);
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
