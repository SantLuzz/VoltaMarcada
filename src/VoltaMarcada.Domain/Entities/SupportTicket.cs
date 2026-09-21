using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.Exceptions;

namespace VoltaMarcada.Domain.Entities;

public sealed class SupportTicket
{
    private readonly List<SupportTicketMessage> messages = [];
    private SupportTicket() { }
    public Guid Id { get; private set; }
    public long Number { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid RequesterUserId { get; private set; }
    public string RequesterName { get; private set; } = string.Empty;
    public string RequesterEmail { get; private set; } = string.Empty;
    public string CompanyTradeName { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public SupportTicketStatus Status { get; private set; }
    public DateTimeOffset OpenedAtUtc { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }
    public Guid? ClosedByUserId { get; private set; }
    public IReadOnlyCollection<SupportTicketMessage> Messages => messages;

    public static SupportTicket Create(long number, Guid companyId, Guid requesterUserId, string requesterName, string requesterEmail, string companyTradeName, string subject, string message, DateTimeOffset openedAtUtc)
    {
        if (number <= 0 || companyId == Guid.Empty || requesterUserId == Guid.Empty) throw new DomainException("Ticket number, company ID, and requester ID are required.");
        if (string.IsNullOrWhiteSpace(requesterName) || string.IsNullOrWhiteSpace(requesterEmail) || string.IsNullOrWhiteSpace(companyTradeName)) throw new DomainException("Requester and company details are required.");
        if (string.IsNullOrWhiteSpace(subject) || subject.Trim().Length > 200 || string.IsNullOrWhiteSpace(message) || message.Trim().Length > 5000) throw new DomainException("Ticket subject or message is invalid.");
        var ticket = new SupportTicket { Id = Guid.NewGuid(), Number = number, CompanyId = companyId, RequesterUserId = requesterUserId, RequesterName = requesterName.Trim(), RequesterEmail = requesterEmail.Trim(), CompanyTradeName = companyTradeName.Trim(), Subject = subject.Trim(), Status = SupportTicketStatus.Open, OpenedAtUtc = openedAtUtc.ToUniversalTime() };
        ticket.messages.Add(SupportTicketMessage.Create(ticket.Id, SupportTicketMessageDirection.Customer, message, requesterUserId, openedAtUtc));
        return ticket;
    }

    public SupportTicketMessage AddPlatformMessage(string body, Guid sentByUserId, DateTimeOffset createdAtUtc)
    {
        if (Status != SupportTicketStatus.Open) throw new DomainException("Only an open ticket can receive a response.");
        var message = SupportTicketMessage.Create(Id, SupportTicketMessageDirection.Platform, body, sentByUserId, createdAtUtc);
        messages.Add(message);
        return message;
    }

    public void Close(Guid closedByUserId, DateTimeOffset closedAtUtc)
    {
        if (Status != SupportTicketStatus.Open || closedByUserId == Guid.Empty) throw new DomainException("Only an open ticket can be closed by an administrator.");
        Status = SupportTicketStatus.Closed; ClosedByUserId = closedByUserId; ClosedAtUtc = closedAtUtc.ToUniversalTime();
    }
}
