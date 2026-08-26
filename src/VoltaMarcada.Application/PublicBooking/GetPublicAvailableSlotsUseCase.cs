using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Scheduling;

namespace VoltaMarcada.Application.PublicBooking;

public sealed record PublicProfessionalDto(Guid Id, string Name);

public sealed record PublicAppointmentSlotDto(Guid ProfessionalId, string ProfessionalName, TimeOnly StartTime);

public sealed class GetPublicAvailableSlotsUseCase(
    ICompanyRepository companyRepository,
    IServiceRepository serviceRepository,
    IProfessionalRepository professionalRepository,
    IMaintenanceOpportunityRepository opportunityRepository,
    IAppointmentRequestRepository requestRepository,
    AppointmentAvailability availability,
    ISchedulingConfiguration configuration,
    TimeProvider timeProvider)
{
    public async Task<(IReadOnlyList<PublicProfessionalDto> Professionals, IReadOnlyList<PublicAppointmentSlotDto> Slots)> ExecuteAsync(
        string? companySlug,
        Guid serviceId,
        DateOnly date,
        Guid? professionalId = null,
        CancellationToken cancellationToken = default)
    {
        var company = await companyRepository.GetBySlugAsync(companySlug?.Trim().ToLowerInvariant() ?? string.Empty, cancellationToken);
        if (company is null || serviceId == Guid.Empty || date == DateOnly.MinValue)
        {
            return ([], []);
        }

        var service = await serviceRepository.GetForCompanyAsync(serviceId, company.Id, cancellationToken);
        if (service is null || !service.IsActive || !service.AllowOnlineBooking)
        {
            return ([], []);
        }

        return await GetSlotsAsync(company.Id, service.Id, service.DurationMinutes, date, professionalId, cancellationToken);
    }

    public async Task<(IReadOnlyList<PublicProfessionalDto> Professionals, IReadOnlyList<PublicAppointmentSlotDto> Slots)> ExecuteContextualAsync(
        string? token,
        DateOnly date,
        Guid? professionalId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return ([], []);
        var context = await opportunityRepository.GetBookingContextAsync(PublicTokenGenerator.Hash(token), cancellationToken);
        var opportunity = context?.Opportunity;
        if (context is null || opportunity is null || !context.ServiceIsActive || opportunity.BookingTokenExpiresAtUtc <= timeProvider.GetUtcNow() ||
            opportunity.Status != VoltaMarcada.Domain.Enums.MaintenanceOpportunityStatus.BookingRequested)
            return ([], []);
        return await GetSlotsAsync(opportunity.CompanyId, opportunity.RecommendedServiceId, context.ServiceDurationMinutes, date, professionalId, cancellationToken);
    }

    public async Task<(IReadOnlyList<PublicProfessionalDto> Professionals, IReadOnlyList<PublicAppointmentSlotDto> Slots)> ExecuteResponseAsync(
        string? token,
        DateOnly date,
        Guid? professionalId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return ([], []);
        var request = await requestRepository.GetForResponseTokenAsync(PublicTokenGenerator.Hash(token), cancellationToken);
        if (request is null || request.ResponseTokenExpiresAtUtc <= timeProvider.GetUtcNow() ||
            request.Status != VoltaMarcada.Domain.Enums.AppointmentRequestStatus.RescheduleRequested)
            return ([], []);
        var service = await serviceRepository.GetForCompanyAsync(request.ServiceId, request.CompanyId, cancellationToken);
        if (service is null || !service.IsActive) return ([], []);
        return await GetSlotsAsync(request.CompanyId, request.ServiceId, request.ProposedDurationMinutes ?? service.DurationMinutes, date, professionalId, cancellationToken);
    }

    private async Task<(IReadOnlyList<PublicProfessionalDto> Professionals, IReadOnlyList<PublicAppointmentSlotDto> Slots)> GetSlotsAsync(
        Guid companyId,
        Guid serviceId,
        int durationMinutes,
        DateOnly date,
        Guid? professionalId,
        CancellationToken cancellationToken)
    {

        var candidates = (await professionalRepository.GetAllForCompanyAsync(companyId, null, cancellationToken))
            .Where(item => item.Professional.IsActive && item.ServiceIds.Contains(serviceId))
            .Where(item => professionalId is null || item.Professional.Id == professionalId)
            .ToArray();
        var professionals = candidates.Select(item => new PublicProfessionalDto(item.Professional.Id, item.CompanyMemberName)).ToArray();
        var slots = new List<PublicAppointmentSlotDto>();

        foreach (var candidate in candidates)
        {
            for (var minute = 0; minute < 24 * 60; minute += configuration.SlotIntervalMinutes)
            {
                var startTime = new TimeOnly(minute / 60, minute % 60);
                if (AppointmentAvailability.TryGetUtcInterval(candidate.Professional.TimeZoneId, date, startTime, durationMinutes, out var startAtUtc, out _) &&
                    startAtUtc > timeProvider.GetUtcNow() &&
                    await availability.IsAvailableAsync(candidate.Professional, date, startTime, durationMinutes, cancellationToken: cancellationToken))
                {
                    slots.Add(new PublicAppointmentSlotDto(candidate.Professional.Id, candidate.CompanyMemberName, startTime));
                }
            }
        }

        return (professionals, slots.OrderBy(slot => slot.StartTime).ThenBy(slot => slot.ProfessionalName).ToArray());
    }
}
