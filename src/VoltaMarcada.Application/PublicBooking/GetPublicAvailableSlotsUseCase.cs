using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.PublicBooking;

public sealed record PublicAppointmentSlotDto(TimeOnly StartTime);

public sealed class GetPublicAvailableSlotsUseCase(
    ICompanyRepository companyRepository,
    IServiceRepository serviceRepository,
    IMaintenanceOpportunityRepository opportunityRepository,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<PublicAppointmentSlotDto>> ExecuteAsync(string? companySlug, Guid serviceId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var company = await companyRepository.GetBySlugAsync(companySlug?.Trim().ToLowerInvariant() ?? string.Empty, cancellationToken);
        if (company is null || serviceId == Guid.Empty || date == DateOnly.MinValue) return [];
        var service = await serviceRepository.GetForCompanyAsync(serviceId, company.Id, cancellationToken);
        return service is null || !service.IsActive || !service.AllowOnlineBooking ? [] : GetSlots(company, date);
    }

    public async Task<IReadOnlyList<PublicAppointmentSlotDto>> ExecuteContextualAsync(string? token, DateOnly date, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return [];
        var context = await opportunityRepository.GetBookingContextAsync(PublicTokenGenerator.Hash(token), cancellationToken);
        var opportunity = context?.Opportunity;
        if (context is null || opportunity is null || !context.ServiceIsActive || opportunity.BookingTokenExpiresAtUtc <= timeProvider.GetUtcNow() || opportunity.Status != MaintenanceOpportunityStatus.BookingRequested)
            return [];
        var company = await companyRepository.GetByIdAsync(opportunity.CompanyId, cancellationToken);
        return company is null ? [] : GetSlots(company, date);
    }

    private static IReadOnlyList<PublicAppointmentSlotDto> GetSlots(Company company, DateOnly date)
    {
        var hours = company.BusinessHours.SingleOrDefault(item => item.DayOfWeek == date.DayOfWeek);
        if (hours is not { IsOpen: true, StartTime: { } startTime, EndTime: { } endTime }) return [];

        var slots = new List<PublicAppointmentSlotDto>();
        for (var time = startTime; time < endTime; time = time.AddMinutes(company.PublicRequestIntervalMinutes))
            slots.Add(new PublicAppointmentSlotDto(time));
        return slots;
    }
}
