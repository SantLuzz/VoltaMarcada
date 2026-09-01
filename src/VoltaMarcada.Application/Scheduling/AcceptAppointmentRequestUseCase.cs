using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.WorkOrders;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.Scheduling;

public sealed class AcceptAppointmentRequestUseCase(
    WorkOrderAuthorization authorization,
    IAppointmentRequestRepository requestRepository,
    IProfessionalRepository professionalRepository,
    IServiceRepository serviceRepository,
    IAppointmentRepository appointmentRepository,
    IMaintenanceOpportunityRepository opportunityRepository,
    ICustomerRepository customerRepository,
    AppointmentAvailability availability,
    TimeProvider timeProvider)
{
    public async Task<OperationResult<Guid>> ExecuteAsync(Guid requestId, bool applyCustomerProfileProposal = false, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var request = await requestRepository.GetForCompanyAsync(requestId, scope.CompanyId, cancellationToken);
        if (request is null) return OperationResult<Guid>.Failure(SchedulingErrors.NotFound);
        if (request.ProfessionalId is not { } professionalId) return OperationResult<Guid>.Failure(SchedulingErrors.InvalidStatus);
        if (!scope.IsOwner && scope.ProfessionalId != professionalId) return OperationResult<Guid>.Failure(SchedulingErrors.Forbidden);
        if (request.Status != AppointmentRequestStatus.Pending) return OperationResult<Guid>.Failure(SchedulingErrors.InvalidStatus);
        var service = await serviceRepository.GetForCompanyAsync(request.ServiceId, scope.CompanyId, cancellationToken);
        if (service is null || !service.IsActive || service.DurationMinutes != request.DurationMinutes) return OperationResult<Guid>.Failure(SchedulingErrors.InvalidInput);
        var professional = await professionalRepository.GetForCompanyAsync(professionalId, scope.CompanyId, cancellationToken);
        if (professional is null || professional.ProfessionalServices.All(link => link.ServiceId != request.ServiceId)) return OperationResult<Guid>.Failure(SchedulingErrors.ProfessionalNotQualified);
        await using var transaction = await appointmentRepository.BeginSchedulingTransactionAsync(scope.CompanyId, professional.Id, cancellationToken);
        if (!await availability.IsAvailableAsync(professional, request.RequestedDate, request.RequestedStartTime, request.DurationMinutes, cancellationToken: cancellationToken)) return OperationResult<Guid>.Failure(SchedulingErrors.ProfessionalUnavailable);
        if (!AppointmentAvailability.TryGetUtcInterval(professional.TimeZoneId, request.RequestedDate, request.RequestedStartTime, request.DurationMinutes, out var start, out var end)) return OperationResult<Guid>.Failure(SchedulingErrors.ProfessionalUnavailable);
        if (start <= timeProvider.GetUtcNow()) return OperationResult<Guid>.Failure(SchedulingErrors.ProfessionalUnavailable);
        var appointment = Appointment.Create(scope.CompanyId, professional.Id, request.CustomerId, request.EquipmentId, request.ServiceId, null, start, end, null, timeProvider.GetUtcNow(), AppointmentOrigin.External, AppointmentType.Visit);
        await appointmentRepository.AddAsync(appointment, cancellationToken);
        request.Accept(appointment.Id, timeProvider.GetUtcNow());
        if (request.MaintenanceOpportunityId is { } opportunityId)
        {
            var opportunity = await opportunityRepository.GetForCompanyAsync(opportunityId, scope.CompanyId, cancellationToken);
            if (opportunity is not null && opportunity.Status == MaintenanceOpportunityStatus.BookingRequested)
                opportunity.MarkAsBooked(timeProvider.GetUtcNow());
        }
        if (applyCustomerProfileProposal && scope.IsOwner && request.ProposedCustomerName is not null &&
            request.ProposedCustomerWhatsApp is not null && request.ProposedAddressType is { } addressType &&
            request.ProposedAddressStreet is not null && request.ProposedAddressNumber is not null &&
            request.ProposedAddressPostalCode is not null && request.ProposedAddressStateId is { } stateId &&
            request.ProposedAddressCityId is { } cityId)
        {
            var customer = await customerRepository.GetForCompanyAsync(request.CustomerId, scope.CompanyId, cancellationToken);
            if (customer is not null)
            {
                var previousPrimaryAddress = customer.Addresses.Single(address => address.IsPrimary);
                var remainingAddresses = customer.Addresses.Where(address => !address.IsPrimary).ToArray();
                var proposedPrimaryAddress = CustomerAddress.Create(addressType, true, request.ProposedAddressStreet, request.ProposedAddressNumber, request.ProposedAddressPostalCode, request.ProposedAddressComplement, stateId, cityId);
                customerRepository.RemoveAddress(previousPrimaryAddress);
                customer.Update(request.ProposedCustomerName, customer.Document, request.ProposedCustomerWhatsApp, request.ProposedCustomerEmail, customer.Notes, timeProvider.GetUtcNow());
                customer.ReplaceAddresses([proposedPrimaryAddress, .. remainingAddresses]);
            }
        }
        if (await appointmentRepository.TrySaveAppointmentChangesAsync(cancellationToken) == AppointmentPersistenceOutcome.Conflict) return OperationResult<Guid>.Failure(SchedulingErrors.AppointmentConflict);
        await transaction.CommitAsync(cancellationToken);
        return OperationResult<Guid>.Success(appointment.Id);
    }
}
