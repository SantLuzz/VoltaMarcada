namespace VoltaMarcada.Application.CompanySettings;

public sealed record UpdateCompanyPublicSettingsCommand(
    string? PublicDescription,
    string? PublicPhone,
    string? NotificationEmail);
