using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.Exceptions;
using Xunit;

namespace VoltaMarcada.IntegrationTests;

public sealed class MaintenanceOpportunityLifecycleTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 2, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Convert_WhenOpen_RecordsTheEntityReferenceAndMakesTheOpportunityTerminal()
    {
        var opportunity = CreateOpportunity();
        var appointmentId = Guid.NewGuid();

        opportunity.Convert(MaintenanceOpportunityConversionType.Appointment, appointmentId, Now);

        Assert.Equal(MaintenanceOpportunityStatus.Converted, opportunity.Status);
        Assert.Equal(MaintenanceOpportunityConversionType.Appointment, opportunity.ConversionType);
        Assert.Equal(appointmentId, opportunity.ConvertedEntityId);
        Assert.Equal(Now, opportunity.ConvertedAtUtc);
        Assert.Throws<DomainException>(() => opportunity.MarkAsAwaitingCustomer(Now));
    }

    [Fact]
    public void CloseWithoutConversion_WhenReasonIsBlank_Throws()
    {
        var opportunity = CreateOpportunity();

        Assert.Throws<DomainException>(() => opportunity.CloseWithoutConversion(Guid.NewGuid(), " ", Now));
    }

    [Fact]
    public void CloseWithoutConversion_WhenReasonIsProvided_RecordsClosureTraceability()
    {
        var opportunity = CreateOpportunity();
        var userId = Guid.NewGuid();

        opportunity.CloseWithoutConversion(userId, "Customer declined the offer.", Now);

        Assert.Equal(MaintenanceOpportunityStatus.ClosedWithoutConversion, opportunity.Status);
        Assert.Equal(userId, opportunity.ClosedByUserId);
        Assert.Equal("Customer declined the offer.", opportunity.ClosureReason);
        Assert.Equal(Now, opportunity.ClosedAtUtc);
    }

    private static MaintenanceOpportunity CreateOpportunity() => MaintenanceOpportunity.Create(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        new DateOnly(2026, 10, 1),
        "Maintenance reminder",
        Now);
}
