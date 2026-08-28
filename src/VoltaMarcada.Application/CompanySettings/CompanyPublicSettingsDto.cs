namespace VoltaMarcada.Application.CompanySettings;

public sealed record CompanyPublicSettingsDto(
    string Slug,
    string TradeName,
    string? PublicDescription,
    string? PublicPhone,
    string? NotificationEmail,
    int PublicRequestIntervalMinutes,
    IReadOnlyList<CompanyBusinessHoursDto> BusinessHours);

public sealed record CompanyBusinessHoursDto(DayOfWeek DayOfWeek, bool IsOpen, TimeOnly? StartTime, TimeOnly? EndTime);
