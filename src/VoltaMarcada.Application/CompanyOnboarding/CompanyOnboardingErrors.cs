using VoltaMarcada.Application.Abstractions.Operations;

namespace VoltaMarcada.Application.CompanyOnboarding;

public static class CompanyOnboardingErrors
{
    public static readonly OperationError Unauthenticated =
        new("CompanyOnboarding.Unauthenticated", OperationErrorType.Forbidden, "An authenticated user is required.");

    public static readonly OperationError AlreadyOnboarded =
        new("CompanyOnboarding.AlreadyOnboarded", OperationErrorType.Conflict, "The current user already belongs to a company.");

    public static readonly OperationError InvalidInput =
        new("CompanyOnboarding.InvalidInput", OperationErrorType.Validation, "Company onboarding input is invalid.");

    public static readonly OperationError DocumentAlreadyExists =
        new("CompanyOnboarding.DocumentAlreadyExists", OperationErrorType.Conflict, "A company with this document already exists.");

    public static readonly OperationError SlugAlreadyExists =
        new("CompanyOnboarding.SlugAlreadyExists", OperationErrorType.Conflict, "A company with this slug already exists.");

    public static readonly OperationError InvalidLocation =
        new("CompanyOnboarding.InvalidLocation", OperationErrorType.Validation, "The selected company location is invalid.");

    public static readonly OperationError InvalidLogo =
        new("CompanyOnboarding.InvalidLogo", OperationErrorType.Validation, "The company logo is invalid.");
}
