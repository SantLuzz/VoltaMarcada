namespace VoltaMarcada.Domain.Enums;

public enum ServiceRequestStatus
{
    Pending,
    QuoteInProgress,
    QuoteSent,
    ConvertedToWorkOrder,
    Closed,
    Cancelled
}
