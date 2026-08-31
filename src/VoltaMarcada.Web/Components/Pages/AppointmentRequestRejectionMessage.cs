using VoltaMarcada.Application.Scheduling;

namespace VoltaMarcada.Web.Components.Pages;

public static class AppointmentRequestRejectionMessage
{
    public static string Create(AppointmentRequestDto request, string reason)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        return $"Olá, {request.CustomerName}!\n\n" +
               $"Sua solicitação para {request.ServiceName} em {request.Date:dd/MM/yyyy} às {request.StartTime:HH:mm} não pôde ser atendida.\n\n" +
               $"Motivo: {reason.Trim()}";
    }
}
