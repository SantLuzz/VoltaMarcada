namespace VoltaMarcada.Application.CompanySettings;

public sealed record CompanyPublicSettingsDto(
    string Slug,
    string TradeName,
    string? PublicDescription,
    string? PublicPhone,
    string? NotificationEmail);
