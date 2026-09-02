using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Communication;
using VoltaMarcada.Application.Abstractions.Routing;
using VoltaMarcada.Application.Customers;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.ValueObjects;
using FluentValidation;

namespace VoltaMarcada.Application.PublicBooking;

public sealed class SubmitPublicAppointmentRequestUseCase(
    ICompanyRepository companyRepository,
    IServiceRepository serviceRepository,
    ICustomerRepository customerRepository,
    IEquipmentRepository equipmentRepository,
    IAppointmentRequestRepository requestRepository,
    IServiceRequestRepository serviceRequestRepository,
    IValidator<CreateCustomerCommand> customerValidator,
    IPublicBookingSubmissionLimiter submissionLimiter,
    IPublicBookingNotificationSender notificationSender,
    IApplicationUrlBuilder applicationUrlBuilder,
    TimeProvider timeProvider)
{
    public async Task<OperationResult<Guid>> ExecuteAsync(SubmitPublicAppointmentRequestCommand command, CancellationToken cancellationToken = default)
    {
        var customerCommand = new CreateCustomerCommand(command.CustomerName, command.CustomerDocument, command.CustomerWhatsApp, command.CustomerEmail, null, command.Addresses);
        var validation = await customerValidator.ValidateAsync(customerCommand, cancellationToken);
        if (!validation.IsValid || string.IsNullOrWhiteSpace(command.CompanySlug) ||
            string.IsNullOrWhiteSpace(command.EquipmentType) || command.EquipmentType.Trim().Length > 200 ||
            command.EquipmentManufacturer?.Trim().Length > 200 ||
            !BrazilianDocument.TryNormalize(command.CustomerDocument, out var document))
            return OperationResult<Guid>.Failure(PublicBookingErrors.InvalidInput);
        if (!submissionLimiter.TryAcquire(PublicTokenGenerator.Hash($"{command.CompanySlug.Trim().ToLowerInvariant()}:{document}")))
            return OperationResult<Guid>.Failure(PublicBookingErrors.RateLimited);

        var company = await companyRepository.GetBySlugAsync(command.CompanySlug.Trim().ToLowerInvariant(), cancellationToken);
        if (company is null) return OperationResult<Guid>.Failure(PublicBookingErrors.NotFound);
        var service = await serviceRepository.GetForCompanyAsync(command.ServiceId, company.Id, cancellationToken);
        if (service is null || !service.IsActive || !service.AllowOnlineBooking || !company.IsPublicRequestSlot(command.Date, command.StartTime) || !PublicRequestSlotTime.IsFuture(command.Date, command.StartTime, timeProvider))
            return OperationResult<Guid>.Failure(PublicBookingErrors.InvalidInput);
        if (!await customerRepository.AreLocationsValidAsync(command.Addresses, cancellationToken))
            return OperationResult<Guid>.Failure(PublicBookingErrors.InvalidInput);
        var customer = await customerRepository.GetByDocumentAsync(company.Id, document, cancellationToken);
        var primaryAddress = command.Addresses.Single(address => address.IsPrimary);
        if (!BrazilianPostalCode.TryNormalize(primaryAddress.PostalCode, out var normalizedPostalCode))
            return OperationResult<Guid>.Failure(PublicBookingErrors.InvalidInput);
        CustomerProfileProposal? proposal = new(
            command.CustomerName,
            command.CustomerWhatsApp,
            command.CustomerEmail,
            new CustomerAddressProfileProposal(
                primaryAddress.AddressType,
                primaryAddress.Street,
                primaryAddress.Number,
                normalizedPostalCode,
                primaryAddress.Complement,
                primaryAddress.StateId,
                primaryAddress.CityId));
        if (customer is null)
        {
            customer = Customer.Create(company.Id, command.CustomerName, document, command.CustomerWhatsApp, command.CustomerEmail, null, CreateCustomerUseCase.CreateAddresses(command.Addresses), timeProvider.GetUtcNow());
            await customerRepository.AddAsync(customer, cancellationToken);
            proposal = null;
        }
        if (command.ProblemDescription?.Trim().Length > 2000) return OperationResult<Guid>.Failure(PublicBookingErrors.InvalidInput);
        var now = timeProvider.GetUtcNow();
        var equipment = await equipmentRepository.GetActiveForCustomerByTypeAndManufacturerAsync(
            company.Id,
            customer.Id,
            command.EquipmentType,
            command.EquipmentManufacturer,
            cancellationToken);
        if (equipment is null)
        {
            equipment = global::VoltaMarcada.Domain.Entities.Equipment.Create(
                company.Id,
                customer.Id,
                command.EquipmentType,
                command.EquipmentManufacturer,
                null,
                null,
                null,
                null,
                null,
                null,
                now);
            await equipmentRepository.AddAsync(equipment, cancellationToken);
        }
        var request = AppointmentRequest.CreatePublicPreference(company.Id, customer.Id, service.Id, equipment.Id, null, command.Date, command.StartTime, service.DurationMinutes, proposal, now);
        var serviceRequest = ServiceRequest.Create(company.Id, customer.Id, request.Id, command.ProblemDescription, now);
        await requestRepository.AddAsync(request, cancellationToken);
        await serviceRequestRepository.AddAsync(serviceRequest, cancellationToken);
        await requestRepository.SaveChangesAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(company.NotificationEmail))
        {
            await notificationSender.SendNewAppointmentRequestAsync(
                company.NotificationEmail,
                company.TradeName ?? company.LegalName,
                customer.Name,
                service.Name,
                command.Date,
                command.StartTime,
                applicationUrlBuilder.BuildApplicationUrl($"solicitacoes/{request.Id}"),
                cancellationToken);
        }
        return OperationResult<Guid>.Success(request.Id);
    }
}
