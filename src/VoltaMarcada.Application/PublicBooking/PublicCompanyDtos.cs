namespace VoltaMarcada.Application.PublicBooking;

public sealed record PublicCompanyPageDto(
    string TradeName,
    string? LogoKey,
    string? Description,
    string WhatsApp,
    string? Phone,
    IReadOnlyList<PublicBookingServiceDto> Services);

public sealed record PublicBookingServiceDto(Guid Id, string Name, string? Description, int DurationMinutes);
