using VoltaMarcada.Domain.Abstractions.Tenancy;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.Exceptions;

namespace VoltaMarcada.Domain.Entities;

public sealed class CustomerContact : ICompanyScopedEntity
{
    private CustomerContact()
    {
    }

    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid OpportunityId { get; private set; }
    public CustomerContactChannel Channel { get; private set; }
    public CustomerContactStatus Status { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? SentAtUtc { get; private set; }

    public static CustomerContact Prepare(
        Guid companyId,
        Guid customerId,
        Guid opportunityId,
        CustomerContactChannel channel,
        string message,
        DateTimeOffset now)
    {
        RequireId(companyId, "Company");
        RequireId(customerId, "Customer");
        RequireId(opportunityId, "Opportunity");
        var utcNow = now.ToUniversalTime();

        return new CustomerContact
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            CustomerId = customerId,
            OpportunityId = opportunityId,
            Channel = channel,
            Status = CustomerContactStatus.Prepared,
            Message = NormalizeMessage(message),
            CreatedAtUtc = utcNow
        };
    }

    public void MarkAsSent(DateTimeOffset now)
    {
        if (Status != CustomerContactStatus.Prepared)
        {
            throw new DomainException("Customer contact cannot be marked as sent in its current status.");
        }

        Status = CustomerContactStatus.Sent;
        SentAtUtc = now.ToUniversalTime();
    }

    private static void RequireId(Guid value, string name)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException($"{name} ID is required.");
        }
    }

    private static string NormalizeMessage(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (normalized is null || normalized.Length > 2000)
        {
            throw new DomainException("Customer contact message is required and cannot exceed 2000 characters.");
        }

        return normalized;
    }
}
