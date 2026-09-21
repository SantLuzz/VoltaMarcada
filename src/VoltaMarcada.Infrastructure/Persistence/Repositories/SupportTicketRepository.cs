using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Infrastructure.Persistence.Repositories;

public sealed class SupportTicketRepository(AppDbContext dbContext) : ISupportTicketRepository
{
    public async Task<ISupportTicketTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        new SupportTicketTransaction(await dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken));

    public async Task<long> GetNextNumberAsync(CancellationToken cancellationToken = default) =>
        (await dbContext.SupportTickets.MaxAsync(ticket => (long?)ticket.Number, cancellationToken) ?? 0) + 1;

    public Task AddAsync(SupportTicket ticket, CancellationToken cancellationToken = default) =>
        dbContext.SupportTickets.AddAsync(ticket, cancellationToken).AsTask();

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => dbContext.SaveChangesAsync(cancellationToken);

    private sealed class SupportTicketTransaction(IDbContextTransaction transaction) : ISupportTicketTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default) => transaction.CommitAsync(cancellationToken);
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}