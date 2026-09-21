using System.Globalization;
using System.Net;
using VoltaMarcada.Application.Abstractions.Communication;

namespace VoltaMarcada.Infrastructure.Communication;

public sealed class PublicBookingNotificationSender(IEmailSender emailSender) : IPublicBookingNotificationSender
{
    public async Task SendNewAppointmentRequestAsync(
        string recipientEmail,
        string companyDisplayName,
        string customerName,
        string serviceName,
        DateOnly requestedDate,
        TimeOnly requestedStartTime,
        string agendaUrl,
        CancellationToken cancellationToken)
    {
        var culture = CultureInfo.GetCultureInfo("pt-BR");
        var safeCompanyName = WebUtility.HtmlEncode(companyDisplayName);
        var safeCustomerName = WebUtility.HtmlEncode(customerName);
        var safeServiceName = WebUtility.HtmlEncode(serviceName);
        var safeAgendaUrl = WebUtility.HtmlEncode(agendaUrl);
        var requestedAt = $"{requestedDate.ToString("d", culture)} \u00e0s {requestedStartTime:HH\\:mm}";
        var email = new EmailMessage(
            recipientEmail,
            $"Nova solicita\u00e7\u00e3o de agendamento - {companyDisplayName}",
            $"<p>Ol\u00e1, {safeCompanyName}.</p><p>Uma nova solicita\u00e7\u00e3o de agendamento foi recebida.</p><p><strong>Cliente:</strong> {safeCustomerName}<br /><strong>Servi\u00e7o:</strong> {safeServiceName}<br /><strong>Prefer\u00eancia:</strong> {requestedAt}</p><p><a href=\"{safeAgendaUrl}\">Abrir solicita\u00e7\u00e3o na agenda</a></p>");

        await emailSender.SendAsync(email, cancellationToken);
    }
}
