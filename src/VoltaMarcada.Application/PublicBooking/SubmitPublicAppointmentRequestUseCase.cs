using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Customers;
using VoltaMarcada.Application.Scheduling;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.ValueObjects;
using FluentValidation;

namespace VoltaMarcada.Application.PublicBooking;

public sealed class SubmitPublicAppointmentRequestUseCase(
    ICompanyRepository companyRepository,
    IServiceRepository serviceRepository,
    IProfessionalRepository professionalRepository,
    ICustomerRepository customerRepository,
    IAppointmentRequestRepository requestRepository,
    IServiceRequestRepository serviceRequestRepository,
    AppointmentAvailability availability,
    IValidator<CreateCustomerCommand> customerValidator,
    IPublicBookingSubmissionLimiter submissionLimiter,
    TimeProvider timeProvider)
{
    public async Task<OperationResult<Guid>> ExecuteAsync(SubmitPublicAppointmentRequestCommand command, CancellationToken cancellationToken = default)
    {
        var customerCommand = new CreateCustomerCommand(command.CustomerName, command.CustomerDocument, command.CustomerWhatsApp, command.CustomerEmail, null, command.Addresses);
        var validation = await customerValidator.ValidateAsync(customerCommand, cancellationToken);
        if (!validation.IsValid || string.IsNullOrWhiteSpace(command.CompanySlug) || !BrazilianDocument.TryNormalize(command.CustomerDocument, out var document))
            return OperationResult<Guid>.Failure(PublicBookingErrors.InvalidInput);
        if (!submissionLimiter.TryAcquire(PublicTokenGenerator.Hash($"{command.CompanySlug.Trim().ToLowerInvariant()}:{document}")))
            return OperationResult<Guid>.Failure(PublicBookingErrors.RateLimited);

        var company = await companyRepository.GetBySlugAsync(command.CompanySlug.Trim().ToLowerInvariant(), cancellationToken);
        if (company is null) return OperationResult<Guid>.Failure(PublicBookingErrors.NotFound);
        var service = await serviceRepository.GetForCompanyAsync(command.ServiceId, company.Id, cancellationToken);
        var professional = await professionalRepository.GetForCompanyAsync(command.ProfessionalId, company.Id, cancellationToken);
        if (service is null || !service.IsActive || !service.AllowOnlineBooking || professional is null || !professional.IsActive || professional.ProfessionalServices.All(x => x.ServiceId != service.Id))
            return OperationResult<Guid>.Failure(PublicBookingErrors.InvalidInput);
        if (!await customerRepository.AreLocationsValidAsync(command.Addresses, cancellationToken) ||
            !AppointmentAvailability.TryGetUtcInterval(professional.TimeZoneId, command.Date, command.StartTime, service.DurationMinutes, out var start, out var end) ||
            start <= timeProvider.GetUtcNow() ||
            !await availability.IsAvailableAsync(professional, command.Date, command.StartTime, service.DurationMinutes, cancellationToken: cancellationToken))
            return OperationResult<Guid>.Failure(PublicBookingErrors.InvalidInput);
        var customer = await customerRepository.GetByDocumentAsync(company.Id, document, cancellationToken);
        var primaryAddress = command.Addresses.Single(address => address.IsPrimary);
        CustomerProfileProposal? proposal = new(
            command.CustomerName,
            command.CustomerWhatsApp,
            command.CustomerEmail,
            new CustomerAddressProfileProposal(
                primaryAddress.AddressType,
                primaryAddress.Street,
                primaryAddress.Number,
                primaryAddress.PostalCode,
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
        var request = AppointmentRequest.Create(company.Id, customer.Id, service.Id, professional.Id, null, null, command.Date, command.StartTime, service.DurationMinutes, start, end, proposal, now, command.ProblemDescription);
        var serviceRequest = ServiceRequest.Create(company.Id, customer.Id, request.Id, command.ProblemDescription, now);
        await requestRepository.AddAsync(request, cancellationToken);
        await serviceRequestRepository.AddAsync(serviceRequest, cancellationToken);
        await requestRepository.SaveChangesAsync(cancellationToken);
        return OperationResult<Guid>.Success(request.Id);
    }
}
