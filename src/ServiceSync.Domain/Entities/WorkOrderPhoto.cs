using ServiceSync.Domain.Enums;
using ServiceSync.Domain.Exceptions;

namespace ServiceSync.Domain.Entities;

public sealed class WorkOrderPhoto
{
    private WorkOrderPhoto() { }

    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid WorkOrderId { get; private set; }
    public WorkOrderPhotoType Type { get; private set; }
    public string FileKey { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid UploadedByUserId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    internal static WorkOrderPhoto Create(Guid companyId, Guid workOrderId, WorkOrderPhotoType type, string fileKey, string? description, Guid uploadedByUserId, DateTimeOffset now)
    {
        if (companyId == Guid.Empty || workOrderId == Guid.Empty || uploadedByUserId == Guid.Empty) throw new DomainException("Company, work order, and uploader IDs are required.");
        if (!Enum.IsDefined(type)) throw new DomainException("Work order photo type is invalid.");
        if (string.IsNullOrWhiteSpace(fileKey)) throw new DomainException("Photo file key is required.");
        return new WorkOrderPhoto { Id = Guid.NewGuid(), CompanyId = companyId, WorkOrderId = workOrderId, Type = type, FileKey = fileKey.Trim(), Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(), UploadedByUserId = uploadedByUserId, CreatedAtUtc = now.ToUniversalTime() };
    }
}
