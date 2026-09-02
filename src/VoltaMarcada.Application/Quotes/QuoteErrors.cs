using VoltaMarcada.Application.Abstractions.Operations;
namespace VoltaMarcada.Application.Quotes;
public static class QuoteErrors
{
    public static readonly OperationError InvalidInput = new("Quote.InvalidInput", OperationErrorType.Validation, "Quote input is invalid.");
    public static readonly OperationError NotFound = new("Quote.NotFound", OperationErrorType.NotFound, "Quote was not found.");
    public static readonly OperationError InvalidStatus = new("Quote.InvalidStatus", OperationErrorType.Conflict, "Quote state does not allow this action.");
}
