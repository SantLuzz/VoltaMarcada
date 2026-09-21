using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.Exceptions;

namespace VoltaMarcada.Domain.Entities;

public sealed class SupportTicketMessage
{
        private readonly List<SupportTicketAttachment> attachments = [];

    private SupportTicketMessage() { }
    public Guid Id { get; private set; }
    public Guid SupportTicketId { get; private set; }
    public SupportTicketMessageDirection Direction { get; private set; }
    public string Body { get; private set; } = string.Empty;
    public Guid? SentByUserId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public IReadOnlyCollection<SupportTicketAttachment> Attachments => attachments;
    internal static SupportTicketMessage Create(Guid ticketId, SupportTicketMessageDirection direction, string body, Guid sentByUserId, DateTimeOffset createdAtUtc)
    {
        if (ticketId == Guid.Empty || sentByUserId == Guid.Empty || string.IsNullOrWhiteSpace(body) || body.Trim().Length > 5000) throw new DomainException("Ticket message is invalid.");
        return new SupportTicketMessage { Id = Guid.NewGuid(), SupportTicketId = ticketId, Direction = direction, Body = body.Trim(), SentByUserId = sentByUserId, CreatedAtUtc = createdAtUtc.ToUniversalTime() };
    }
}
