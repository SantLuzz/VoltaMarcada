using VoltaMarcada.Application.Abstractions.Operations;

namespace VoltaMarcada.Application.Scheduling;

public static class SchedulingErrors
{
    public static readonly OperationError InvalidInput =
        new("Scheduling.InvalidInput", OperationErrorType.Validation, "Scheduling input is invalid.");

    public static readonly OperationError NotFound =
        new("Scheduling.NotFound", OperationErrorType.NotFound, "The requested scheduling record was not found.");

    public static readonly OperationError Forbidden =
        new("Scheduling.Forbidden", OperationErrorType.Forbidden, "The current user cannot perform this scheduling operation.");

    public static readonly OperationError ProfessionalUnavailable =
        new("Scheduling.ProfessionalUnavailable", OperationErrorType.Conflict, "The professional is unavailable for the requested interval.");

    public static readonly OperationError ProfessionalNotQualified =
        new("Scheduling.ProfessionalNotQualified", OperationErrorType.Validation, "The professional is not qualified for the selected service.");

    public static readonly OperationError AppointmentConflict =
        new("Scheduling.AppointmentConflict", OperationErrorType.Conflict, "The appointment conflicts with another calendar entry.");

    public static readonly OperationError InvalidStatus =
        new("Scheduling.InvalidStatus", OperationErrorType.Conflict, "The scheduling record does not allow this operation in its current state.");

    public static readonly OperationError BlockRequestRequiresAppointmentResolution =
        new("Scheduling.BlockRequestRequiresAppointmentResolution", OperationErrorType.Conflict, "Appointments must be resolved before approving the schedule block request.");
}
