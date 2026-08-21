namespace ServiceSync.Application.Professionals;

public sealed record ProfessionalAvailabilityOperationResult(
    ProfessionalAvailabilityOperationResultCode Code,
    Guid? ScheduleBlockId = null)
{
    public static ProfessionalAvailabilityOperationResult Success(Guid? scheduleBlockId = null) =>
        new(ProfessionalAvailabilityOperationResultCode.Success, scheduleBlockId);

    public static ProfessionalAvailabilityOperationResult InvalidInput() =>
        new(ProfessionalAvailabilityOperationResultCode.InvalidInput);

    public static ProfessionalAvailabilityOperationResult ProfessionalNotFound() =>
        new(ProfessionalAvailabilityOperationResultCode.ProfessionalNotFound);

    public static ProfessionalAvailabilityOperationResult ScheduleBlockNotFound() =>
        new(ProfessionalAvailabilityOperationResultCode.ScheduleBlockNotFound);

    public static ProfessionalAvailabilityOperationResult Forbidden() =>
        new(ProfessionalAvailabilityOperationResultCode.Forbidden);
}

public enum ProfessionalAvailabilityOperationResultCode
{
    Success,
    InvalidInput,
    ProfessionalNotFound,
    ScheduleBlockNotFound,
    Forbidden
}
