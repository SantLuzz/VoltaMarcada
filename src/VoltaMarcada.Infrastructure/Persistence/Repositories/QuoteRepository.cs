using Microsoft.EntityFrameworkCore;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Infrastructure.Persistence.Repositories;

public sealed class QuoteRepository(AppDbContext dbContext) : IQuoteRepository
{
    public Task AddAsync(Quote quote, CancellationToken cancellationToken = default) => dbContext.Quotes.AddAsync(quote, cancellationToken).AsTask();
    public Task<Quote?> GetForCompanyAsync(Guid quoteId, Guid companyId, CancellationToken cancellationToken = default) =>
        dbContext.Quotes.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == quoteId && x.CompanyId == companyId, cancellationToken);
    public Task<Quote?> GetForPublicTokenAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        dbContext.Quotes.Include(x => x.Items).SingleOrDefaultAsync(x => x.PublicTokenHash == tokenHash && x.PublicTokenRevokedAtUtc == null, cancellationToken);
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => dbContext.SaveChangesAsync(cancellationToken);
}
