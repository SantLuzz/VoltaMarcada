using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Support;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Infrastructure.Persistence.Repositories;

public sealed class SupportTicketRepository(AppDbContext dbContext) : ISupportTicketRepository
{
    public async Task<ISupportTicketTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        new SupportTicketTransaction(await dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken));

    public async Task<long> GetNextNumberAsync(CancellationToken cancellationToken = default) =>
        (await dbContext.SupportTickets.MaxAsync(ticket => (long?)ticket.Number, cancellationToken) ?? 0) + 1;

    public Task AddAsync(SupportTicket ticket, CancellationToken cancellationToken = default) =>
        dbContext.SupportTickets.AddAsync(ticket, cancellationToken).AsTask();

    public async Task<IReadOnlyList<PlatformSupportTicketListItem>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.SupportTickets.AsNoTracking()
            .OrderBy(ticket => ticket.Status == SupportTicketStatus.Closed)
            .ThenByDescending(ticket => ticket.OpenedAtUtc)
            .Select(ticket => new PlatformSupportTicketListItem(ticket.Id, ticket.Number, ticket.CompanyTradeName, ticket.RequesterName, ticket.RequesterEmail, ticket.Subject, ticket.Status, ticket.OpenedAtUtc))
            .ToArrayAsync(cancellationToken);

    public Task<SupportTicket?> GetForUpdateAsync(Guid ticketId, CancellationToken cancellationToken = default) =>
        dbContext.SupportTickets.Include(ticket => ticket.Messages).ThenInclude(message => message.Attachments)
            .SingleOrDefaultAsync(ticket => ticket.Id == ticketId, cancellationToken);

    public Task<SupportTicketAttachment?> GetAttachmentAsync(Guid attachmentId, CancellationToken cancellationToken = default) =>
        dbContext.SupportTicketAttachments.AsNoTracking().SingleOrDefaultAsync(attachment => attachment.Id == attachmentId, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => dbContext.SaveChangesAsync(cancellationToken);

    private sealed class SupportTicketTransaction(IDbContextTransaction transaction) : ISupportTicketTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default) => transaction.CommitAsync(cancellationToken);
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}