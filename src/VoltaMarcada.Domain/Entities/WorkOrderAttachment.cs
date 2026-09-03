using VoltaMarcada.Domain.Exceptions;

namespace VoltaMarcada.Domain.Entities;

public sealed class WorkOrderAttachment
{
    private WorkOrderAttachment() { }

    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid WorkOrderId { get; private set; }
    public string FileKey { get; private set; } = string.Empty;
    public string OriginalFileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long Length { get; private set; }
    public string? Description { get; private set; }
    public Guid UploadedByUserId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    internal static WorkOrderAttachment Create(Guid companyId, Guid workOrderId, string fileKey, string originalFileName, string contentType, long length, string? description, Guid uploadedByUserId, DateTimeOffset now)
    {
        if (companyId == Guid.Empty || workOrderId == Guid.Empty || uploadedByUserId == Guid.Empty) throw new DomainException("Company, work order, and uploader IDs are required.");
        if (string.IsNullOrWhiteSpace(fileKey) || fileKey.Trim().Length > 1000) throw new DomainException("Attachment file key is required and cannot exceed 1000 characters.");
        if (string.IsNullOrWhiteSpace(originalFileName) || originalFileName.Trim().Length > 255) throw new DomainException("Attachment file name is required and cannot exceed 255 characters.");
        if (string.IsNullOrWhiteSpace(contentType) || contentType.Trim().Length > 255) throw new DomainException("Attachment content type is required and cannot exceed 255 characters.");
        if (length <= 0) throw new DomainException("Attachment length must be positive.");
        var normalizedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (normalizedDescription?.Length > 1000) throw new DomainException("Attachment description cannot exceed 1000 characters.");
        return new WorkOrderAttachment { Id = Guid.NewGuid(), CompanyId = companyId, WorkOrderId = workOrderId, FileKey = fileKey.Trim(), OriginalFileName = originalFileName.Trim(), ContentType = contentType.Trim(), Length = length, Description = normalizedDescription, UploadedByUserId = uploadedByUserId, CreatedAtUtc = now.ToUniversalTime() };
    }
}
