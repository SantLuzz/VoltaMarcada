using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Scheduling;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.PublicBooking;

public sealed record SubmitContextualAppointmentRequestCommand(
    string Token,
    Guid ProfessionalId,
    DateOnly Date,
    TimeOnly StartTime);

public sealed class SubmitContextualAppointmentRequestUseCase(
    IMaintenanceOpportunityRepository opportunityRepository,
    IProfessionalRepository professionalRepository,
    IAppointmentRequestRepository requestRepository,
    AppointmentAvailability availability,
    TimeProvider timeProvider)
{
    public async Task<OperationResult<Guid>> ExecuteAsync(
        SubmitContextualAppointmentRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Token))
            return OperationResult<Guid>.Failure(PublicBookingErrors.NotFound);

        var context = await opportunityRepository.GetBookingContextAsync(PublicTokenGenerator.Hash(command.Token), cancellationToken);
        var opportunity = context?.Opportunity;
        if (context is null || opportunity is null || !context.ServiceIsActive || opportunity.BookingTokenExpiresAtUtc <= timeProvider.GetUtcNow() ||
            opportunity.Status != MaintenanceOpportunityStatus.BookingRequested)
            return OperationResult<Guid>.Failure(PublicBookingErrors.NotFound);
        if (await requestRepository.HasOpenForMaintenanceOpportunityAsync(opportunity.CompanyId, opportunity.Id, cancellationToken))
            return OperationResult<Guid>.Failure(PublicBookingErrors.InvalidInput);

        var professional = await professionalRepository.GetForCompanyAsync(command.ProfessionalId, opportunity.CompanyId, cancellationToken);
        if (professional is null || !professional.IsActive || professional.ProfessionalServices.All(link => link.ServiceId != opportunity.RecommendedServiceId) ||
            !AppointmentAvailability.TryGetUtcInterval(professional.TimeZoneId, command.Date, command.StartTime, context.ServiceDurationMinutes, out var start, out var end) ||
            start <= timeProvider.GetUtcNow() ||
            !await availability.IsAvailableAsync(professional, command.Date, command.StartTime, context.ServiceDurationMinutes, cancellationToken: cancellationToken))
            return OperationResult<Guid>.Failure(PublicBookingErrors.InvalidInput);

        var request = AppointmentRequest.Create(
            opportunity.CompanyId,
            opportunity.CustomerId,
            opportunity.RecommendedServiceId,
            professional.Id,
            opportunity.EquipmentId,
            opportunity.Id,
            command.Date,
            command.StartTime,
            context.ServiceDurationMinutes,
            start,
            end,
            null,
            timeProvider.GetUtcNow());
        await requestRepository.AddAsync(request, cancellationToken);
        opportunity.RevokeBookingToken(timeProvider.GetUtcNow());
        await requestRepository.SaveChangesAsync(cancellationToken);
        return OperationResult<Guid>.Success(request.Id);
    }
}
