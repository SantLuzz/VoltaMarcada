using VoltaMarcada.Application.Abstractions.Persistence;

namespace VoltaMarcada.Application.Scheduling;

public sealed class AnalyzeAppointmentRequestAvailabilityUseCase(
    IAppointmentRequestRepository requestRepository,
    IProfessionalRepository professionalRepository,
    AppointmentAvailability availability,
    ISchedulingConfiguration configuration,
    TimeProvider timeProvider)
{
    public async Task<AppointmentRequestAvailabilityAnalysis?> ExecuteAsync(Guid requestId, Guid companyId, CancellationToken cancellationToken = default)
    {
        var request = await requestRepository.GetForCompanyAsync(requestId, companyId, cancellationToken);
        if (request is null) return null;
        var candidates = (await professionalRepository.GetAllForCompanyAsync(companyId, null, cancellationToken))
            .Where(item => item.Professional.IsActive && item.ServiceIds.Contains(request.ServiceId))
            .ToArray();
        var exact = new List<AvailableProfessionalDto>();
        foreach (var candidate in candidates)
        {
            if (await IsFutureAvailableAsync(candidate.Professional, request.RequestedDate, request.RequestedStartTime, request.DurationMinutes, cancellationToken))
                exact.Add(new AvailableProfessionalDto(candidate.Professional.Id, candidate.CompanyMemberName));
        }

        if (exact.Count > 0) return new AppointmentRequestAvailabilityAnalysis(exact, []);
        var alternatives = new List<AppointmentAlternativeDto>();
        for (var offset = 0; offset < configuration.AvailabilitySearchDays && alternatives.Count < configuration.AvailabilityResultLimit; offset++)
        {
            var date = request.RequestedDate.AddDays(offset);
            for (var minutes = offset == 0 ? request.RequestedStartTime.Hour * 60 + request.RequestedStartTime.Minute + configuration.SlotIntervalMinutes : 0;
                 minutes < 24 * 60 && alternatives.Count < configuration.AvailabilityResultLimit;
                 minutes += configuration.SlotIntervalMinutes)
            {
                var startTime = new TimeOnly(minutes / 60, minutes % 60);
                foreach (var candidate in candidates)
                {
                    if (await IsFutureAvailableAsync(candidate.Professional, date, startTime, request.DurationMinutes, cancellationToken))
                    {
                        alternatives.Add(new AppointmentAlternativeDto(candidate.Professional.Id, candidate.CompanyMemberName, date, startTime));
                        if (alternatives.Count == configuration.AvailabilityResultLimit) break;
                    }
                }
            }
        }

        return new AppointmentRequestAvailabilityAnalysis([], alternatives);
    }

    private async Task<bool> IsFutureAvailableAsync(VoltaMarcada.Domain.Entities.Professional professional, DateOnly date, TimeOnly startTime, int durationMinutes, CancellationToken cancellationToken) =>
        AppointmentAvailability.TryGetUtcInterval(professional.TimeZoneId, date, startTime, durationMinutes, out var startAtUtc, out _) &&
        startAtUtc > timeProvider.GetUtcNow() &&
        await availability.IsAvailableAsync(professional, date, startTime, durationMinutes, cancellationToken: cancellationToken);
}
