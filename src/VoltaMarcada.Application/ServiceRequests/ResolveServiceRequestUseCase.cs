using VoltaMarcada.Application.Abstractions.Identity;
using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Scheduling;
using VoltaMarcada.Application.WorkOrders;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.ValueObjects;

namespace VoltaMarcada.Application.ServiceRequests;

public sealed class ResolveServiceRequestUseCase(
    WorkOrderAuthorization authorization,
    IAppointmentRequestRepository appointmentRequests,
    IServiceRequestRepository serviceRequests,
    IQuoteRepository quotes,
    IAppointmentRepository appointments,
    IProfessionalRepository professionals,
    IServiceRepository services,
    IEquipmentRepository equipment,
    IWorkOrderRepository workOrders,
    ICustomerRepository customers,
    AppointmentAvailability availability,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
{
    public async Task<OperationResult<ServiceRequestResolutionResult>> ExecuteAsync(
        ResolveServiceRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.AppointmentRequestId == Guid.Empty || command.ProfessionalId == Guid.Empty || command.EquipmentId == Guid.Empty ||
            string.IsNullOrWhiteSpace(command.AppointmentDescription) || command.AppointmentDescription.Trim().Length > 1000 ||
            !Enum.IsDefined(command.Resolution))
        {
            return OperationResult<ServiceRequestResolutionResult>.Failure(ServiceRequestErrors.InvalidInput);
        }

        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var appointmentRequest = await appointmentRequests.GetForCompanyAsync(command.AppointmentRequestId, scope.CompanyId, cancellationToken);
        var serviceRequest = await serviceRequests.GetForAppointmentRequestAsync(command.AppointmentRequestId, scope.CompanyId, cancellationToken);
        if (appointmentRequest is null || serviceRequest is null)
        {
            return OperationResult<ServiceRequestResolutionResult>.Failure(ServiceRequestErrors.NotFound);
        }

        if (!scope.IsOwner && scope.ProfessionalId != command.ProfessionalId)
        {
            return OperationResult<ServiceRequestResolutionResult>.Failure(ServiceRequestErrors.Forbidden);
        }

        if (appointmentRequest.Status != AppointmentRequestStatus.Pending || serviceRequest.Status != ServiceRequestStatus.Pending)
        {
            return OperationResult<ServiceRequestResolutionResult>.Failure(ServiceRequestErrors.InvalidStatus);
        }

        var professional = await professionals.GetForCompanyAsync(command.ProfessionalId, scope.CompanyId, cancellationToken);
        var service = await services.GetForCompanyAsync(appointmentRequest.ServiceId, scope.CompanyId, cancellationToken);
        var selectedEquipment = await equipment.GetForCompanyAsync(command.EquipmentId, scope.CompanyId, cancellationToken);
        if (professional is null || service is null || !service.IsActive || selectedEquipment is null ||
            selectedEquipment.CustomerId != appointmentRequest.CustomerId || !professional.IsActive ||
            professional.ProfessionalServices.All(item => item.ServiceId != service.Id))
        {
            return OperationResult<ServiceRequestResolutionResult>.Failure(ServiceRequestErrors.InvalidInput);
        }

        var serviceUnitPrice = service.PriceType == ServicePriceType.Fixed ? service.BasePrice : command.ServiceUnitPrice;
        if (serviceUnitPrice is null || serviceUnitPrice < 0)
        {
            return OperationResult<ServiceRequestResolutionResult>.Failure(ServiceRequestErrors.InvalidInput);
        }

        if (!AppointmentAvailability.TryGetUtcInterval(professional.TimeZoneId, appointmentRequest.RequestedDate, appointmentRequest.RequestedStartTime, appointmentRequest.DurationMinutes, out var startAtUtc, out var endAtUtc) ||
            startAtUtc <= timeProvider.GetUtcNow() ||
            !await availability.IsAvailableAsync(professional, appointmentRequest.RequestedDate, appointmentRequest.RequestedStartTime, appointmentRequest.DurationMinutes, cancellationToken: cancellationToken))
        {
            return OperationResult<ServiceRequestResolutionResult>.Failure(ServiceRequestErrors.ProfessionalUnavailable);
        }

        await using var transaction = await appointments.BeginSchedulingTransactionAsync(scope.CompanyId, professional.Id, cancellationToken);

        var now = timeProvider.GetUtcNow();
        Guid? quoteId = null;
        Guid? workOrderId = null;
        AppointmentType appointmentType;

        if (command.Resolution == ServiceRequestResolution.PrepareQuote)
        {
            var quote = Quote.Create(
                scope.CompanyId,
                appointmentRequest.CustomerId,
                currentUser.UserId,
                selectedEquipment.Id,
                service.Id,
                serviceRequest.Id,
                [new QuoteItemDefinition(service.Name, 1, serviceUnitPrice.Value, QuoteItemType.Service)],
                0,
                null,
                DateOnly.FromDateTime(now.UtcDateTime).AddDays(30),
                now);
            serviceRequest.StartQuote(quote.Id, now);
            await quotes.AddAsync(quote, cancellationToken);
            quoteId = quote.Id;
            appointmentType = AppointmentType.TechnicalVisit;
        }
        else
        {
            var workOrder = WorkOrder.Create(
                scope.CompanyId,
                appointmentRequest.CustomerId,
                selectedEquipment.Id,
                service.Id,
                professional.Id,
                currentUser.UserId,
                WorkOrderPriority.Normal,
                serviceRequest.ProblemDescription,
                [new WorkOrderItemDefinition(service.Name, 1, serviceUnitPrice.Value, WorkOrderItemType.Service)],
                service.ChecklistItems.OrderBy(item => item.DisplayOrder).Select(item => new WorkOrderChecklistItemDefinition(item.Description, item.IsRequired, item.DisplayOrder)).ToArray(),
                now);
            serviceRequest.SetDirectWorkOrder(workOrder.Id, now);
            await workOrders.AddAsync(workOrder, cancellationToken);
            workOrderId = workOrder.Id;
            appointmentType = AppointmentType.ServiceExecution;
        }

        var appointment = Appointment.Create(
            scope.CompanyId,
            professional.Id,
            appointmentRequest.CustomerId,
            selectedEquipment.Id,
            service.Id,
            workOrderId,
            startAtUtc,
            endAtUtc,
            null,
            now,
            AppointmentOrigin.External,
            appointmentType,
            command.AppointmentDescription);
        await appointments.AddAsync(appointment, cancellationToken);
        appointmentRequest.AssignProfessional(professional.Id, now);
        appointmentRequest.Accept(appointment.Id, now);

        if (command.ApplyCustomerProfileProposal && scope.IsOwner)
        {
            await ApplyCustomerProfileProposalAsync(appointmentRequest, scope.CompanyId, now, cancellationToken);
        }

        if (await appointments.TrySaveAppointmentChangesAsync(cancellationToken) == AppointmentPersistenceOutcome.Conflict)
        {
            return OperationResult<ServiceRequestResolutionResult>.Failure(ServiceRequestErrors.ProfessionalUnavailable);
        }

        await transaction.CommitAsync(cancellationToken);
        return OperationResult<ServiceRequestResolutionResult>.Success(new ServiceRequestResolutionResult(appointment.Id, quoteId, workOrderId));
    }

    private async Task ApplyCustomerProfileProposalAsync(
        AppointmentRequest appointmentRequest,
        Guid companyId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (appointmentRequest.ProposedCustomerName is null ||
            appointmentRequest.ProposedCustomerWhatsApp is null ||
            appointmentRequest.ProposedAddressType is not { } addressType ||
            appointmentRequest.ProposedAddressStreet is null ||
            appointmentRequest.ProposedAddressNumber is null ||
            appointmentRequest.ProposedAddressPostalCode is null ||
            appointmentRequest.ProposedAddressStateId is not { } stateId ||
            appointmentRequest.ProposedAddressCityId is not { } cityId)
        {
            return;
        }

        var customer = await customers.GetForCompanyAsync(appointmentRequest.CustomerId, companyId, cancellationToken);
        if (customer is null)
        {
            return;
        }

        var previousPrimaryAddress = customer.Addresses.Single(address => address.IsPrimary);
        var remainingAddresses = customer.Addresses.Where(address => !address.IsPrimary).ToArray();
        var proposedPrimaryAddress = CustomerAddress.Create(
            addressType,
            true,
            appointmentRequest.ProposedAddressStreet,
            appointmentRequest.ProposedAddressNumber,
            appointmentRequest.ProposedAddressPostalCode,
            appointmentRequest.ProposedAddressComplement,
            stateId,
            cityId);

        customers.RemoveAddress(previousPrimaryAddress);
        customer.Update(
            appointmentRequest.ProposedCustomerName,
            customer.Document,
            appointmentRequest.ProposedCustomerWhatsApp,
            appointmentRequest.ProposedCustomerEmail,
            customer.Notes,
            now);
        customer.ReplaceAddresses([proposedPrimaryAddress, .. remainingAddresses]);
    }
}
