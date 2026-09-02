using VoltaMarcada.Application.Scheduling;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Web.Components.Pages;
using Xunit;

namespace VoltaMarcada.IntegrationTests.Components;

public sealed class AppointmentRequestRejectionMessageTests
{
    [Fact]
    public void Create_ShouldIncludeRequestContextAndReason()
    {
        var request = new AppointmentRequestDto(
            Guid.NewGuid(),
            "João",
            "11999999999",
            Guid.NewGuid(),
            "Limpeza de ar-condicionado",
            null,
            string.Empty,
            new DateOnly(2026, 9, 1),
            new TimeOnly(8, 30),
            60,
            AppointmentRequestStatus.Pending,
            false,
            false,
            null,
            null,
            null,
            null,
            [],
            []);

        var message = AppointmentRequestRejectionMessage.Create(
            request,
            "Não temos atendimento nessa região.");

        Assert.Equal(
            "Olá, João!\n\nSua solicitação para Limpeza de ar-condicionado em 01/09/2026 às 08:30 não pôde ser atendida.\n\nMotivo: Não temos atendimento nessa região.",
            message);
    }

    [Fact]
    public void Create_WhenReasonIsBlank_ShouldRejectMessageCreation()
    {
        var request = new AppointmentRequestDto(
            Guid.NewGuid(), "João", "11999999999", Guid.NewGuid(), "Limpeza",
            null, string.Empty, new DateOnly(2026, 9, 1), new TimeOnly(8, 30), 60,
            AppointmentRequestStatus.Pending, false, false, null, null, null, null, [], []);

        Assert.Throws<ArgumentException>(() =>
            AppointmentRequestRejectionMessage.Create(request, "   "));
    }
}
