using VoltaMarcada.Application.Abstractions.Operations;

namespace VoltaMarcada.Application.Professionals;

public static class ProfessionalErrors
{
    public static readonly OperationError InvalidInput =
        new("Professional.InvalidInput", OperationErrorType.Validation, "Professional input is invalid.");

    public static readonly OperationError NotFound =
        new("Professional.NotFound", OperationErrorType.NotFound, "Professional was not found.");

    public static readonly OperationError InvalidCompanyMember =
        new("Professional.InvalidCompanyMember", OperationErrorType.Validation, "The selected company member is invalid.");

    public static readonly OperationError InvalidService =
        new("Professional.InvalidService", OperationErrorType.Validation, "One or more selected services are invalid.");

    public static readonly OperationError Forbidden =
        new("Professional.Forbidden", OperationErrorType.Forbidden, "The current user cannot manage professionals.");

    public static readonly OperationError AvailabilityInvalidInput =
        new("ProfessionalAvailability.InvalidInput", OperationErrorType.Validation, "Professional availability input is invalid.");

    public static readonly OperationError AvailabilityProfessionalNotFound =
        new("ProfessionalAvailability.ProfessionalNotFound", OperationErrorType.NotFound, "Professional was not found.");

    public static readonly OperationError ScheduleBlockNotFound =
        new("ProfessionalAvailability.ScheduleBlockNotFound", OperationErrorType.NotFound, "Schedule block was not found.");

    public static readonly OperationError AvailabilityForbidden =
        new("ProfessionalAvailability.Forbidden", OperationErrorType.Forbidden, "The current user cannot manage this availability.");

    public static readonly OperationError ScheduleBlockConflictsWithAppointment =
        new("ProfessionalAvailability.ScheduleBlockConflictsWithAppointment", OperationErrorType.Conflict, "The schedule block overlaps an open appointment.");
}
