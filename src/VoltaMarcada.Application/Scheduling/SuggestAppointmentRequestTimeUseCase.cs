using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.PublicBooking;
using VoltaMarcada.Application.WorkOrders;
using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.Scheduling;

public sealed record SuggestAppointmentRequestTimeCommand(Guid ProfessionalId, DateOnly Date, TimeOnly StartTime);

public sealed class SuggestAppointmentRequestTimeUseCase(
    WorkOrderAuthorization authorization,
    IAppointmentRequestRepository requestRepository,
    IProfessionalRepository professionalRepository,
    IServiceRepository serviceRepository,
    AppointmentAvailability availability,
    IPublicBookingConfiguration configuration,
    TimeProvider timeProvider)
{
    public async Task<OperationResult<string>> ExecuteAsync(Guid requestId, SuggestAppointmentRequestTimeCommand command, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var request = await requestRepository.GetForCompanyAsync(requestId, scope.CompanyId, cancellationToken);
        if (request is null) return OperationResult<string>.Failure(SchedulingErrors.NotFound);
        if (!scope.IsOwner && scope.ProfessionalId != request.ProfessionalId) return OperationResult<string>.Failure(SchedulingErrors.Forbidden);
        if (!scope.IsOwner && scope.ProfessionalId != command.ProfessionalId) return OperationResult<string>.Failure(SchedulingErrors.Forbidden);
        if (request.Status != AppointmentRequestStatus.Pending) return OperationResult<string>.Failure(SchedulingErrors.InvalidStatus);
        var service = await serviceRepository.GetForCompanyAsync(request.ServiceId, scope.CompanyId, cancellationToken);
        if (service is null || !service.IsActive) return OperationResult<string>.Failure(SchedulingErrors.InvalidInput);
        var professional = await professionalRepository.GetForCompanyAsync(command.ProfessionalId, scope.CompanyId, cancellationToken);
        if (professional is null || !professional.IsActive || professional.ProfessionalServices.All(link => link.ServiceId != request.ServiceId))
            return OperationResult<string>.Failure(SchedulingErrors.ProfessionalNotQualified);
        if (!AppointmentAvailability.TryGetUtcInterval(professional.TimeZoneId, command.Date, command.StartTime, service.DurationMinutes, out var startAtUtc, out _) || startAtUtc <= timeProvider.GetUtcNow())
            return OperationResult<string>.Failure(SchedulingErrors.InvalidInput);
        if (!await availability.IsAvailableAsync(professional, command.Date, command.StartTime, service.DurationMinutes, cancellationToken: cancellationToken))
            return OperationResult<string>.Failure(SchedulingErrors.ProfessionalUnavailable);
        var token = PublicTokenGenerator.Generate();
        request.SuggestNewTime(command.Date, command.StartTime, service.DurationMinutes, professional.Id, PublicTokenGenerator.Hash(token), timeProvider.GetUtcNow().AddDays(configuration.MaintenanceLinkLifetimeDays), timeProvider.GetUtcNow());
        await requestRepository.SaveChangesAsync(cancellationToken);
        return OperationResult<string>.Success(token);
    }
}
