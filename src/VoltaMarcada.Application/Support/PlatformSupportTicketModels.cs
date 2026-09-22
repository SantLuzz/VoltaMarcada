using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.Support;

public sealed record PlatformSupportTicketListItem(Guid Id, long Number, string CompanyTradeName, string RequesterName, string RequesterEmail, string Subject, SupportTicketStatus Status, DateTimeOffset OpenedAtUtc);
public sealed record PlatformSupportTicketDetails(Guid Id, long Number, string CompanyTradeName, string RequesterName, string RequesterEmail, string Subject, SupportTicketStatus Status, DateTimeOffset OpenedAtUtc, DateTimeOffset? ClosedAtUtc, IReadOnlyList<PlatformSupportTicketMessage> Messages);
public sealed record PlatformSupportTicketMessage(Guid Id, SupportTicketMessageDirection Direction, string Body, DateTimeOffset CreatedAtUtc, IReadOnlyList<PlatformSupportTicketAttachment> Attachments);
public sealed record PlatformSupportTicketAttachment(Guid Id, string OriginalFileName, string ContentType, long Length);