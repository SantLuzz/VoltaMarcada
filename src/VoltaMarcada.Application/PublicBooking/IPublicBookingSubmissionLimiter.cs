namespace VoltaMarcada.Application.PublicBooking;

public interface IPublicBookingSubmissionLimiter
{
    bool TryAcquire(string partitionKey);
}
