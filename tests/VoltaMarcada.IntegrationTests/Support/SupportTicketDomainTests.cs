using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.Exceptions;
using Xunit;

namespace VoltaMarcada.IntegrationTests.Support;

public sealed class SupportTicketDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 21, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_AssignsOpenStatusAndInitialCustomerMessage()
    {
        var ticket = SupportTicket.Create(
            42,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Maria Silva",
            "maria@example.com",
            "Empresa Teste",
            "Ajuda",
            "Preciso de ajuda.",
            Now);

        Assert.Equal(SupportTicketStatus.Open, ticket.Status);
        Assert.Equal(42, ticket.Number);
        var message = Assert.Single(ticket.Messages);
        Assert.Equal(SupportTicketMessageDirection.Customer, message.Direction);
        Assert.Equal("Preciso de ajuda.", message.Body);
    }

    [Fact]
    public void AddPlatformMessage_WhenTicketIsClosed_ThrowsDomainException()
    {
        var ticket = SupportTicket.Create(42, Guid.NewGuid(), Guid.NewGuid(), "Maria Silva", "maria@example.com", "Empresa Teste", "Ajuda", "Preciso de ajuda.", Now);
        ticket.Close(Guid.NewGuid(), Now.AddMinutes(1));

        Assert.Throws<DomainException>(() => ticket.AddPlatformMessage("Resposta", Guid.NewGuid(), Now.AddMinutes(2)));
    }
}
