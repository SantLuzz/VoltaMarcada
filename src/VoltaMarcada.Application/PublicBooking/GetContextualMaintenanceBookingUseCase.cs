using VoltaMarcada.Application.Abstractions.Persistence;

namespace VoltaMarcada.Application.PublicBooking;

public sealed record ContextualMaintenanceBookingDto(
    string CompanySlug,
    string CompanyTradeName,
    string? CompanyLogoKey,
    string CompanyWhatsApp,
    string CustomerName,
    string EquipmentDescription,
    Guid ServiceId,
    string ServiceName,
    int ServiceDurationMinutes);

public sealed class GetContextualMaintenanceBookingUseCase(IMaintenanceOpportunityRepository repository, TimeProvider timeProvider)
{
    public async Task<ContextualMaintenanceBookingDto?> ExecuteAsync(string? token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        var context = await repository.GetBookingContextAsync(PublicTokenGenerator.Hash(token), cancellationToken);
        var opportunity = context?.Opportunity;
        return context is not null && opportunity is not null && context.ServiceIsActive &&
               opportunity.Status == VoltaMarcada.Domain.Enums.MaintenanceOpportunityStatus.BookingRequested &&
               opportunity.BookingTokenExpiresAtUtc > timeProvider.GetUtcNow()
            ? new ContextualMaintenanceBookingDto(
                context.CompanySlug,
                context.CompanyTradeName,
                context.CompanyLogoKey,
                context.CompanyWhatsApp,
                context.CustomerName,
                context.EquipmentDescription,
                opportunity.RecommendedServiceId,
                context.ServiceName,
                context.ServiceDurationMinutes)
            : null;
    }
}
