using ServiceSync.Application.Abstractions.Storage;

namespace ServiceSync.Application.CompanyOnboarding;

public sealed record CompleteCompanyOnboardingCommand(
    string LegalName,
    string TradeName,
    string Document,
    string WhatsApp,
    string PostalCode,
    string Address,
    Guid StateId,
    Guid CityId,
    FileUpload? Logo);
