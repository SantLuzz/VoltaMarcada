using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Application.CompanySettings;

public sealed record UpdateCompanyPublicSettingsCommand(
    string? PublicDescription,
    string? PublicPhone,
    string? NotificationEmail,
    int PublicRequestIntervalMinutes,
    IReadOnlyCollection<CompanyBusinessHoursInput> BusinessHours);
