using VoltaMarcada.Application.Abstractions.Operations;
namespace VoltaMarcada.Application.PublicBooking;
public static class PublicBookingErrors
{
    public static readonly OperationError InvalidInput = new("PublicBooking.InvalidInput", OperationErrorType.Validation, "Public booking input is invalid.");
    public static readonly OperationError NotFound = new("PublicBooking.NotFound", OperationErrorType.NotFound, "Public booking page was not found.");
    public static readonly OperationError RateLimited = new("PublicBooking.RateLimited", OperationErrorType.Conflict, "Too many public booking submissions were attempted.");
}
