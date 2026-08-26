using VoltaMarcada.Domain.Abstractions.Tenancy;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.Exceptions;

namespace VoltaMarcada.Domain.Entities;

public sealed class ServiceRequest : ICompanyScopedEntity
{
    private ServiceRequest() { }

    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid AppointmentRequestId { get; private set; }
    public Guid? QuoteId { get; private set; }
    public Guid? WorkOrderId { get; private set; }
    public string? ProblemDescription { get; private set; }
    public ServiceRequestStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static ServiceRequest Create(Guid companyId, Guid customerId, Guid appointmentRequestId, string? problemDescription, DateTimeOffset now)
    {
        if (companyId == Guid.Empty || customerId == Guid.Empty || appointmentRequestId == Guid.Empty) throw new DomainException("Company, customer, and appointment request IDs are required.");
        var utcNow = now.ToUniversalTime();
        return new ServiceRequest { Id = Guid.NewGuid(), CompanyId = companyId, CustomerId = customerId, AppointmentRequestId = appointmentRequestId, ProblemDescription = Normalize(problemDescription), Status = ServiceRequestStatus.Pending, CreatedAtUtc = utcNow, UpdatedAtUtc = utcNow };
    }

    public void StartQuote(Guid quoteId, DateTimeOffset now)
    {
        if (Status != ServiceRequestStatus.Pending || quoteId == Guid.Empty) throw new DomainException("Service request cannot start a quote in its current state.");
        QuoteId = quoteId; Status = ServiceRequestStatus.QuoteInProgress; UpdatedAtUtc = now.ToUniversalTime();
    }

    public void MarkQuoteSent(DateTimeOffset now)
    {
        if (Status != ServiceRequestStatus.QuoteInProgress) throw new DomainException("Service request quote is not in progress.");
        Status = ServiceRequestStatus.QuoteSent; UpdatedAtUtc = now.ToUniversalTime();
    }

    public void SetDirectWorkOrder(Guid workOrderId, DateTimeOffset now)
    {
        if (Status != ServiceRequestStatus.Pending || workOrderId == Guid.Empty) throw new DomainException("Service request cannot create a work order in its current state.");
        WorkOrderId = workOrderId; Status = ServiceRequestStatus.ConvertedToWorkOrder; UpdatedAtUtc = now.ToUniversalTime();
    }

    public void CompleteFromQuote(Guid workOrderId, DateTimeOffset now)
    {
        if (Status is not (ServiceRequestStatus.QuoteInProgress or ServiceRequestStatus.QuoteSent) || workOrderId == Guid.Empty) throw new DomainException("Service request cannot complete from quote in its current state.");
        WorkOrderId = workOrderId; Status = ServiceRequestStatus.ConvertedToWorkOrder; UpdatedAtUtc = now.ToUniversalTime();
    }

    private static string? Normalize(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (normalized?.Length > 2000) throw new DomainException("Problem description cannot exceed 2000 characters.");
        return normalized;
    }
}
