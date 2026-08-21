using ServiceSync.Domain.Exceptions;

namespace ServiceSync.Domain.Entities;

public sealed class ServiceChecklistItem
{
    private ServiceChecklistItem()
    {
    }

    public Guid Id { get; private set; }

    public Guid CompanyId { get; private set; }

    public Guid ServiceId { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public bool IsRequired { get; private set; }

    public int DisplayOrder { get; private set; }

    public static ServiceChecklistItem Create(string description, bool isRequired, int displayOrder)
    {
        if (displayOrder < 0)
        {
            throw new DomainException("Display order cannot be negative.");
        }

        return new ServiceChecklistItem
        {
            Id = Guid.NewGuid(),
            Description = RequireText(description),
            IsRequired = isRequired,
            DisplayOrder = displayOrder
        };
    }

    internal void AssignToService(Guid companyId, Guid serviceId)
    {
        if (companyId == Guid.Empty)
        {
            throw new DomainException("Company ID is required.");
        }

        if (serviceId == Guid.Empty)
        {
            throw new DomainException("Service ID is required.");
        }

        if (CompanyId != Guid.Empty && CompanyId != companyId)
        {
            throw new DomainException("Checklist item already belongs to another company.");
        }

        if (ServiceId != Guid.Empty && ServiceId != serviceId)
        {
            throw new DomainException("Checklist item already belongs to another service.");
        }

        CompanyId = companyId;
        ServiceId = serviceId;
    }

    private static string RequireText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("Checklist item description is required.");
        }

        return value.Trim();
    }
}
