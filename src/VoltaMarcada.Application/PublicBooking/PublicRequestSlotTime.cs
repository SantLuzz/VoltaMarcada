namespace VoltaMarcada.Application.PublicBooking;

public static class PublicRequestSlotTime
{
    public static bool IsFuture(DateOnly date, TimeOnly startTime, TimeProvider timeProvider) =>
        date.ToDateTime(startTime) > timeProvider.GetLocalNow().DateTime;
}
