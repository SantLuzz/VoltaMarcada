namespace ServiceSync.Application.CompanyOnboarding;

public sealed record CompleteCompanyOnboardingCommand(
    string LegalName,
    string TradeName,
    string Document,
    string Phone,
    string WhatsApp,
    string City,
    string State,
    string? LogoKey);
