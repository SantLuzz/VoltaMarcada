using VoltaMarcada.Domain.Exceptions;

namespace VoltaMarcada.Domain.Entities;

public sealed class SupportTicketAttachment
{
    private SupportTicketAttachment() { }
    public Guid Id { get; private set; }
    public Guid SupportTicketMessageId { get; private set; }
    public string FileKey { get; private set; } = string.Empty;
    public string OriginalFileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long Length { get; private set; }
    internal static SupportTicketAttachment Create(Guid messageId, string fileKey, string fileName, string contentType, long length)
    {
        if (messageId == Guid.Empty || string.IsNullOrWhiteSpace(fileKey) || string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(contentType) || length <= 0) throw new DomainException("Ticket attachment is invalid.");
        return new SupportTicketAttachment { Id = Guid.NewGuid(), SupportTicketMessageId = messageId, FileKey = fileKey.Trim(), OriginalFileName = fileName.Trim(), ContentType = contentType.Trim(), Length = length };
    }
}
