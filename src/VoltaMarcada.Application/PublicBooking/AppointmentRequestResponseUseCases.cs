using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Scheduling;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.PublicBooking;

public sealed record AppointmentRequestResponseDto(
    string CompanySlug,
    string CompanyTradeName,
    string CustomerName,
    Guid ServiceId,
    string ServiceName,
    DateOnly ProposedDate,
    TimeOnly ProposedStartTime,
    int DurationMinutes);

public sealed class GetAppointmentRequestResponseUseCase(IAppointmentRequestRepository repository, TimeProvider timeProvider)
{
    public async Task<AppointmentRequestResponseDto?> ExecuteAsync(string? token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        var context = await repository.GetResponseContextAsync(PublicTokenGenerator.Hash(token), cancellationToken);
        var request = context?.Request;
        return context is not null && request is not null && request.Status == AppointmentRequestStatus.RescheduleRequested &&
               request.ResponseTokenExpiresAtUtc > timeProvider.GetUtcNow() && request.ProposedDate is { } date &&
               request.ProposedStartTime is { } time && request.ProposedDurationMinutes is { } durationMinutes
            ? new AppointmentRequestResponseDto(context.CompanySlug, context.CompanyTradeName, context.CustomerName, request.ServiceId, context.ServiceName, date, time, durationMinutes)
            : null;
    }
}

public sealed record RespondToAppointmentRequestCommand(string Token, bool AcceptProposal);

public sealed class RespondToAppointmentRequestUseCase(
    IAppointmentRequestRepository requestRepository,
    IProfessionalRepository professionalRepository,
    IServiceRepository serviceRepository,
    IAppointmentRepository appointmentRepository,
    IMaintenanceOpportunityRepository opportunityRepository,
    AppointmentAvailability availability,
    TimeProvider timeProvider)
{
    public async Task<OperationResult<Guid>> ExecuteAsync(RespondToAppointmentRequestCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Token)) return OperationResult<Guid>.Failure(PublicBookingErrors.NotFound);
        var request = await requestRepository.GetForResponseTokenAsync(PublicTokenGenerator.Hash(command.Token), cancellationToken);
        if (request is null || request.ResponseTokenExpiresAtUtc <= timeProvider.GetUtcNow() || request.Status != AppointmentRequestStatus.RescheduleRequested)
            return OperationResult<Guid>.Failure(PublicBookingErrors.NotFound);

        if (!command.AcceptProposal)
        {
            request.DeclineProposal(timeProvider.GetUtcNow());
            await requestRepository.SaveChangesAsync(cancellationToken);
            return OperationResult<Guid>.Success(request.Id);
        }

        if (request.ProposedProfessionalId is not { } professionalId || request.ProposedDate is not { } date || request.ProposedStartTime is not { } startTime || request.ProposedDurationMinutes is not { } durationMinutes)
            return OperationResult<Guid>.Failure(PublicBookingErrors.InvalidInput);
        var service = await serviceRepository.GetForCompanyAsync(request.ServiceId, request.CompanyId, cancellationToken);
        var professional = await professionalRepository.GetForCompanyAsync(professionalId, request.CompanyId, cancellationToken);
        if (service is null || !service.IsActive || professional is null || !professional.IsActive || professional.ProfessionalServices.All(link => link.ServiceId != request.ServiceId))
            return OperationResult<Guid>.Failure(PublicBookingErrors.InvalidInput);

        await using var transaction = await appointmentRepository.BeginSchedulingTransactionAsync(request.CompanyId, professional.Id, cancellationToken);
        if (!await availability.IsAvailableAsync(professional, date, startTime, durationMinutes, cancellationToken: cancellationToken) ||
            !AppointmentAvailability.TryGetUtcInterval(professional.TimeZoneId, date, startTime, durationMinutes, out var start, out var end) || start <= timeProvider.GetUtcNow())
            return OperationResult<Guid>.Failure(PublicBookingErrors.InvalidInput);

        var appointment = Appointment.Create(request.CompanyId, professional.Id, request.CustomerId, request.EquipmentId, request.ServiceId, null, start, end, null, timeProvider.GetUtcNow(), AppointmentOrigin.External);
        await appointmentRepository.AddAsync(appointment, cancellationToken);
        request.AcceptProposal(appointment.Id, timeProvider.GetUtcNow());
        if (request.MaintenanceOpportunityId is { } opportunityId)
        {
            var opportunity = await opportunityRepository.GetForCompanyAsync(opportunityId, request.CompanyId, cancellationToken);
            if (opportunity is not null && opportunity.Status == MaintenanceOpportunityStatus.BookingRequested) opportunity.MarkAsBooked(timeProvider.GetUtcNow());
        }

        if (await appointmentRepository.TrySaveAppointmentChangesAsync(cancellationToken) == AppointmentPersistenceOutcome.Conflict)
            return OperationResult<Guid>.Failure(PublicBookingErrors.InvalidInput);
        await transaction.CommitAsync(cancellationToken);
        return OperationResult<Guid>.Success(appointment.Id);
    }
}
