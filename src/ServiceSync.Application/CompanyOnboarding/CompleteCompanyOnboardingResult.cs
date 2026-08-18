namespace ServiceSync.Application.CompanyOnboarding;

public sealed record CompleteCompanyOnboardingResult(
    CompleteCompanyOnboardingResultCode Code,
    Guid? CompanyId)
{
    public static CompleteCompanyOnboardingResult Unauthenticated() =>
        new(CompleteCompanyOnboardingResultCode.Unauthenticated, null);

    public static CompleteCompanyOnboardingResult AlreadyOnboarded() =>
        new(CompleteCompanyOnboardingResultCode.AlreadyOnboarded, null);

    public static CompleteCompanyOnboardingResult InvalidInput() =>
        new(CompleteCompanyOnboardingResultCode.InvalidInput, null);

    public static CompleteCompanyOnboardingResult DocumentAlreadyExists() =>
        new(CompleteCompanyOnboardingResultCode.DocumentAlreadyExists, null);

    public static CompleteCompanyOnboardingResult SlugAlreadyExists() =>
        new(CompleteCompanyOnboardingResultCode.SlugAlreadyExists, null);

    public static CompleteCompanyOnboardingResult Success(Guid companyId) =>
        new(CompleteCompanyOnboardingResultCode.Success, companyId);
}

public enum CompleteCompanyOnboardingResultCode
{
    Unauthenticated,
    AlreadyOnboarded,
    InvalidInput,
    DocumentAlreadyExists,
    SlugAlreadyExists,
    InvalidLocation,
    InvalidLogo,
    Success
}
