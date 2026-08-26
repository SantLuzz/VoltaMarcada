using VoltaMarcada.Domain.Abstractions.Tenancy;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.Exceptions;

namespace VoltaMarcada.Domain.Entities;

public sealed class AppointmentRequest : ICompanyScopedEntity
{
    private AppointmentRequest()
    {
    }

    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid ServiceId { get; private set; }
    public Guid ProfessionalId { get; private set; }
    public Guid? EquipmentId { get; private set; }
    public Guid? MaintenanceOpportunityId { get; private set; }
    public DateOnly RequestedDate { get; private set; }
    public TimeOnly RequestedStartTime { get; private set; }
    public int DurationMinutes { get; private set; }
    public DateTimeOffset RequestedStartAtUtc { get; private set; }
    public DateTimeOffset RequestedEndAtUtc { get; private set; }
    public AppointmentRequestStatus Status { get; private set; }
    public DateOnly? ProposedDate { get; private set; }
    public TimeOnly? ProposedStartTime { get; private set; }
    public int? ProposedDurationMinutes { get; private set; }
    public Guid? ProposedProfessionalId { get; private set; }
    public string? RejectionReason { get; private set; }
    public string? ProposedCustomerName { get; private set; }
    public string? ProposedCustomerWhatsApp { get; private set; }
    public string? ProposedCustomerEmail { get; private set; }
    public CustomerAddressType? ProposedAddressType { get; private set; }
    public string? ProposedAddressStreet { get; private set; }
    public string? ProposedAddressNumber { get; private set; }
    public string? ProposedAddressPostalCode { get; private set; }
    public string? ProposedAddressComplement { get; private set; }
    public Guid? ProposedAddressStateId { get; private set; }
    public Guid? ProposedAddressCityId { get; private set; }
    public string? ResponseTokenHash { get; private set; }
    public DateTimeOffset? ResponseTokenExpiresAtUtc { get; private set; }
    public DateTimeOffset? ResponseTokenRevokedAtUtc { get; private set; }
    public Guid? AppointmentId { get; private set; }
    public string? ProblemDescription { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static AppointmentRequest Create(
        Guid companyId,
        Guid customerId,
        Guid serviceId,
        Guid professionalId,
        Guid? equipmentId,
        Guid? maintenanceOpportunityId,
        DateOnly requestedDate,
        TimeOnly requestedStartTime,
        int durationMinutes,
        DateTimeOffset requestedStartAtUtc,
        DateTimeOffset requestedEndAtUtc,
        CustomerProfileProposal? customerProfileProposal,
        DateTimeOffset now,
        string? problemDescription = null)
    {
        RequireId(companyId, "Company");
        RequireId(customerId, "Customer");
        RequireId(serviceId, "Service");
        RequireId(professionalId, "Professional");
        EnsureSchedule(requestedDate, requestedStartTime, durationMinutes, requestedStartAtUtc, requestedEndAtUtc);
        var utcNow = now.ToUniversalTime();

        return new AppointmentRequest
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            CustomerId = customerId,
            ServiceId = serviceId,
            ProfessionalId = professionalId,
            EquipmentId = NormalizeOptionalId(equipmentId),
            MaintenanceOpportunityId = NormalizeOptionalId(maintenanceOpportunityId),
            RequestedDate = requestedDate,
            RequestedStartTime = requestedStartTime,
            DurationMinutes = durationMinutes,
            RequestedStartAtUtc = requestedStartAtUtc.ToUniversalTime(),
            RequestedEndAtUtc = requestedEndAtUtc.ToUniversalTime(),
            Status = AppointmentRequestStatus.Pending,
            ProposedCustomerName = customerProfileProposal?.Name,
            ProposedCustomerWhatsApp = customerProfileProposal?.WhatsApp,
            ProposedCustomerEmail = customerProfileProposal?.Email,
            ProposedAddressType = customerProfileProposal?.Address.AddressType,
            ProposedAddressStreet = customerProfileProposal?.Address.Street,
            ProposedAddressNumber = customerProfileProposal?.Address.Number,
            ProposedAddressPostalCode = customerProfileProposal?.Address.PostalCode,
            ProposedAddressComplement = customerProfileProposal?.Address.Complement,
            ProposedAddressStateId = customerProfileProposal?.Address.StateId,
            ProposedAddressCityId = customerProfileProposal?.Address.CityId,
            ProblemDescription = NormalizeOptionalText(problemDescription, 2000, "Problem description"),
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public void Accept(Guid appointmentId, DateTimeOffset now)
    {
        EnsureStatus(AppointmentRequestStatus.Pending);
        RequireId(appointmentId, "Appointment");
        AppointmentId = appointmentId;
        Status = AppointmentRequestStatus.Accepted;
        UpdatedAtUtc = now.ToUniversalTime();
    }

    public void Reject(string? reason, DateTimeOffset now)
    {
        if (Status is not AppointmentRequestStatus.Pending and not AppointmentRequestStatus.RescheduleRequested)
        {
            throw new DomainException("Appointment request cannot be rejected in its current status.");
        }

        RejectionReason = NormalizeOptionalText(reason, 1000, "Rejection reason");
        ResponseTokenRevokedAtUtc = ResponseTokenHash is null ? null : now.ToUniversalTime();
        Status = AppointmentRequestStatus.Rejected;
        UpdatedAtUtc = now.ToUniversalTime();
    }

    public void Cancel(DateTimeOffset now)
    {
        if (Status is not AppointmentRequestStatus.Pending and not AppointmentRequestStatus.RescheduleRequested)
        {
            throw new DomainException("Appointment request cannot be cancelled in its current status.");
        }

        ResponseTokenRevokedAtUtc = ResponseTokenHash is null ? null : now.ToUniversalTime();
        Status = AppointmentRequestStatus.Cancelled;
        UpdatedAtUtc = now.ToUniversalTime();
    }

    public void SuggestNewTime(DateOnly date, TimeOnly startTime, int durationMinutes, Guid professionalId, string responseTokenHash, DateTimeOffset responseTokenExpiresAtUtc, DateTimeOffset now)
    {
        EnsureStatus(AppointmentRequestStatus.Pending);
        RequireId(professionalId, "Professional");
        if (date == DateOnly.MinValue || durationMinutes <= 0 || string.IsNullOrWhiteSpace(responseTokenHash) || responseTokenExpiresAtUtc <= now)
        {
            throw new DomainException("Appointment request reschedule proposal is invalid.");
        }

        ProposedDate = date;
        ProposedStartTime = startTime;
        ProposedDurationMinutes = durationMinutes;
        ProposedProfessionalId = professionalId;
        ResponseTokenHash = responseTokenHash.Trim();
        ResponseTokenExpiresAtUtc = responseTokenExpiresAtUtc.ToUniversalTime();
        ResponseTokenRevokedAtUtc = null;
        Status = AppointmentRequestStatus.RescheduleRequested;
        UpdatedAtUtc = now.ToUniversalTime();
    }

    public void ReturnToPending(DateOnly date, TimeOnly startTime, int durationMinutes, Guid professionalId, DateTimeOffset startAtUtc, DateTimeOffset endAtUtc, DateTimeOffset now)
    {
        EnsureStatus(AppointmentRequestStatus.RescheduleRequested);
        RequireId(professionalId, "Professional");
        EnsureSchedule(date, startTime, durationMinutes, startAtUtc, endAtUtc);
        RequestedDate = date;
        RequestedStartTime = startTime;
        DurationMinutes = durationMinutes;
        RequestedStartAtUtc = startAtUtc.ToUniversalTime();
        RequestedEndAtUtc = endAtUtc.ToUniversalTime();
        ProfessionalId = professionalId;
        ResponseTokenRevokedAtUtc = now.ToUniversalTime();
        Status = AppointmentRequestStatus.Pending;
        UpdatedAtUtc = now.ToUniversalTime();
    }

    private void EnsureStatus(AppointmentRequestStatus expectedStatus)
    {
        if (Status != expectedStatus)
        {
            throw new DomainException("Appointment request is not in the required status.");
        }
    }

    private static void EnsureSchedule(DateOnly date, TimeOnly startTime, int durationMinutes, DateTimeOffset startAtUtc, DateTimeOffset endAtUtc)
    {
        if (date == DateOnly.MinValue || durationMinutes <= 0 || endAtUtc <= startAtUtc)
        {
            throw new DomainException("Appointment request schedule is invalid.");
        }
    }

    private static void RequireId(Guid value, string name)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException($"{name} ID is required.");
        }
    }

    private static Guid? NormalizeOptionalId(Guid? value) => value is null || value == Guid.Empty ? null : value;

    private static string? NormalizeOptionalText(string? value, int maximumLength, string fieldName)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (normalized?.Length > maximumLength)
        {
            throw new DomainException($"{fieldName} cannot exceed {maximumLength} characters.");
        }

        return normalized;
    }
}

public sealed record CustomerProfileProposal(
    string Name,
    string WhatsApp,
    string? Email,
    CustomerAddressProfileProposal Address);

public sealed record CustomerAddressProfileProposal(
    CustomerAddressType AddressType,
    string Street,
    string Number,
    string PostalCode,
    string? Complement,
    Guid StateId,
    Guid CityId);
