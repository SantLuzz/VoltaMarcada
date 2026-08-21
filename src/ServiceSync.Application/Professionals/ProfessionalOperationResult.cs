namespace ServiceSync.Application.Professionals;

public sealed record ProfessionalOperationResult(ProfessionalOperationResultCode Code, Guid? ProfessionalId = null)
{
    public static ProfessionalOperationResult Success(Guid professionalId) => new(ProfessionalOperationResultCode.Success, professionalId);
    public static ProfessionalOperationResult InvalidInput() => new(ProfessionalOperationResultCode.InvalidInput);
    public static ProfessionalOperationResult NotFound() => new(ProfessionalOperationResultCode.NotFound);
    public static ProfessionalOperationResult InvalidCompanyMember() => new(ProfessionalOperationResultCode.InvalidCompanyMember);
    public static ProfessionalOperationResult InvalidService() => new(ProfessionalOperationResultCode.InvalidService);
    public static ProfessionalOperationResult Forbidden() => new(ProfessionalOperationResultCode.Forbidden);
}

public enum ProfessionalOperationResultCode
{
    Success,
    InvalidInput,
    NotFound,
    InvalidCompanyMember,
    InvalidService,
    Forbidden
}
