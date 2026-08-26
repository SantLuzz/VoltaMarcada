using VoltaMarcada.Application.Abstractions.Persistence;

namespace VoltaMarcada.Application.PublicBooking;

public sealed class GetPublicCompanyPageUseCase(ICompanyRepository companyRepository, IServiceRepository serviceRepository)
{
    public async Task<PublicCompanyPageDto?> ExecuteAsync(string? slug, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedSlug))
        {
            return null;
        }

        var company = await companyRepository.GetBySlugAsync(normalizedSlug, cancellationToken);
        if (company is null)
        {
            return null;
        }

        var services = await serviceRepository.GetPublicBookingServicesAsync(company.Id, cancellationToken);
        return new PublicCompanyPageDto(
            company.TradeName,
            company.LogoKey,
            company.PublicDescription,
            company.WhatsApp,
            company.PublicPhone,
            services.Select(service => new PublicBookingServiceDto(service.Id, service.Name, service.Description, service.DurationMinutes)).ToArray());
    }
}
