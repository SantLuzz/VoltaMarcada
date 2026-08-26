using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Scheduling;

namespace VoltaMarcada.Application.PublicBooking;

public sealed record AppointmentRequestResponseDto(
    string CompanySlug,
    string CompanyTradeName,
    string CustomerName,
    Guid ServiceId,
    string ServiceName,
    Guid ProfessionalId,
    string ProfessionalName,
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
        return context is not null && request is not null && request.Status == VoltaMarcada.Domain.Enums.AppointmentRequestStatus.RescheduleRequested &&
               request.ResponseTokenExpiresAtUtc > timeProvider.GetUtcNow() && request.ProposedDate is { } date &&
               request.ProposedStartTime is { } time && request.ProposedProfessionalId is { } professionalId &&
               request.ProposedDurationMinutes is { } durationMinutes
            ? new AppointmentRequestResponseDto(context.CompanySlug, context.CompanyTradeName, context.CustomerName, request.ServiceId, context.ServiceName, professionalId, context.ProfessionalName, date, time, durationMinutes)
            : null;
    }
}

public sealed record RespondToAppointmentRequestCommand(string Token, Guid ProfessionalId, DateOnly Date, TimeOnly StartTime);

public sealed class RespondToAppointmentRequestUseCase(
    IAppointmentRequestRepository repository,
    IProfessionalRepository professionalRepository,
    IServiceRepository serviceRepository,
    AppointmentAvailability availability,
    TimeProvider timeProvider)
{
    public async Task<OperationResult<Guid>> ExecuteAsync(RespondToAppointmentRequestCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Token)) return OperationResult<Guid>.Failure(PublicBookingErrors.NotFound);
        var request = await repository.GetForResponseTokenAsync(PublicTokenGenerator.Hash(command.Token), cancellationToken);
        if (request is null || request.ResponseTokenExpiresAtUtc <= timeProvider.GetUtcNow() || request.Status != VoltaMarcada.Domain.Enums.AppointmentRequestStatus.RescheduleRequested)
            return OperationResult<Guid>.Failure(PublicBookingErrors.NotFound);
        var service = await serviceRepository.GetForCompanyAsync(request.ServiceId, request.CompanyId, cancellationToken);
        if (service is null || !service.IsActive) return OperationResult<Guid>.Failure(PublicBookingErrors.InvalidInput);
        var durationMinutes = request.ProposedDurationMinutes ?? request.DurationMinutes;
        var professional = await professionalRepository.GetForCompanyAsync(command.ProfessionalId, request.CompanyId, cancellationToken);
        if (professional is null || !professional.IsActive || professional.ProfessionalServices.All(link => link.ServiceId != request.ServiceId) ||
            !AppointmentAvailability.TryGetUtcInterval(professional.TimeZoneId, command.Date, command.StartTime, durationMinutes, out var start, out var end) ||
            start <= timeProvider.GetUtcNow() ||
            !await availability.IsAvailableAsync(professional, command.Date, command.StartTime, durationMinutes, cancellationToken: cancellationToken))
            return OperationResult<Guid>.Failure(PublicBookingErrors.InvalidInput);
        request.ReturnToPending(command.Date, command.StartTime, durationMinutes, professional.Id, start, end, timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);
        return OperationResult<Guid>.Success(request.Id);
    }
}
