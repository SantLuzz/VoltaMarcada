using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Application.Abstractions.Persistence;

public interface ISupportTicketRepository
{
    Task<ISupportTicketTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task<long> GetNextNumberAsync(CancellationToken cancellationToken = default);
    Task AddAsync(SupportTicket ticket, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface ISupportTicketTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}