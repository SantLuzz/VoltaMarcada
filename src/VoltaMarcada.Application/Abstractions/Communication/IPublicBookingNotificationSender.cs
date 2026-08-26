namespace VoltaMarcada.Application.Abstractions.Communication;

public interface IPublicBookingNotificationSender
{
    Task SendNewAppointmentRequestAsync(
        string recipientEmail,
        string companyDisplayName,
        string customerName,
        string serviceName,
        DateOnly requestedDate,
        TimeOnly requestedStartTime,
        string agendaUrl,
        CancellationToken cancellationToken);
}
