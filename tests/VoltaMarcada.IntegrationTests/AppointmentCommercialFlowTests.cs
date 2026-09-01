using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.Exceptions;
using VoltaMarcada.Domain.ValueObjects;
using Xunit;

namespace VoltaMarcada.IntegrationTests;

public sealed class AppointmentCommercialFlowTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ServiceRequestQuoteApprovalPath_DoesNotRequireAnAppointment()
    {
        var quote = CreateQuote(sourceAppointmentId: null);
        var workOrder = CreateWorkOrder(sourceQuoteId: quote.Id);

        quote.Send("token", Now.AddDays(1), Now);
        quote.Approve(workOrder.Id, Now);

        Assert.Null(quote.SourceAppointmentId);
        Assert.Equal(workOrder.Id, quote.GeneratedWorkOrderId);
        Assert.Equal(quote.Id, workOrder.SourceQuoteId);
        Assert.Null(workOrder.SourceAppointmentId);
    }

    [Fact]
    public void CompletedVisit_ToQuote_ToWorkOrder_ToNewService_PreservesVisitHistory()
    {
        var visit = CreateAppointment(AppointmentType.Visit);
        visit.Start(Now);
        visit.Complete(Now.AddHours(1));
        var quote = CreateQuote(visit.Id);
        var workOrder = CreateWorkOrder(sourceQuoteId: quote.Id);
        var service = CreateAppointment(AppointmentType.Service, workOrder.Id);

        Assert.Equal(AppointmentType.Visit, visit.Type);
        Assert.Equal(AppointmentStatus.Completed, visit.Status);
        Assert.Equal(visit.Id, quote.SourceAppointmentId);
        Assert.Equal(AppointmentType.Service, service.Type);
        Assert.NotEqual(visit.Id, service.Id);
        Assert.Equal(workOrder.Id, service.WorkOrderId);
    }

    [Fact]
    public void CompletedVisit_ToDirectWorkOrder_DoesNotCreateOrTransformAnAppointment()
    {
        var visit = CreateAppointment(AppointmentType.Visit);
        visit.Start(Now);
        visit.Complete(Now.AddHours(1));
        var workOrder = CreateWorkOrder(sourceAppointmentId: visit.Id);

        Assert.Equal(AppointmentType.Visit, visit.Type);
        Assert.Equal(AppointmentStatus.Completed, visit.Status);
        Assert.Equal(visit.Id, workOrder.SourceAppointmentId);
        Assert.Null(workOrder.SourceQuoteId);
    }

    [Fact]
    public void CompletedOther_CanGenerateDirectWorkOrderAndSeparateService()
    {
        var other = CreateAppointment(AppointmentType.Other);
        other.Start(Now);
        other.Complete(Now.AddHours(1));
        var workOrder = CreateWorkOrder(sourceAppointmentId: other.Id);
        var service = CreateAppointment(AppointmentType.Service, workOrder.Id);

        Assert.Equal(AppointmentType.Other, other.Type);
        Assert.Equal(AppointmentStatus.Completed, other.Status);
        Assert.Equal(other.Id, workOrder.SourceAppointmentId);
        Assert.NotEqual(other.Id, service.Id);
    }

    [Fact]
    public void ManualAppointmentTypes_AreSupportedAndServiceRequiresWorkOrder()
    {
        var visit = CreateAppointment(AppointmentType.Visit);
        var other = CreateAppointment(AppointmentType.Other);
        var workOrder = CreateWorkOrder();
        var service = CreateAppointment(AppointmentType.Service, workOrder.Id);

        Assert.Equal(AppointmentType.Visit, visit.Type);
        Assert.Equal(AppointmentType.Other, other.Type);
        Assert.Equal(AppointmentType.Service, service.Type);
        Assert.Throws<DomainException>(() => CreateAppointment(AppointmentType.Service));
    }

    private static Appointment CreateAppointment(AppointmentType type, Guid? workOrderId = null) => Appointment.Create(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        workOrderId,
        Now.AddDays(1),
        Now.AddDays(1).AddHours(1),
        null,
        Now,
        AppointmentOrigin.Internal,
        type);

    private static Quote CreateQuote(Guid? sourceAppointmentId = null) => Quote.Create(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        null,
        sourceAppointmentId,
        [new QuoteItemDefinition("Service", 1, 150, QuoteItemType.Service)],
        0,
        null,
        new DateOnly(2026, 9, 30),
        Now);

    private static WorkOrder CreateWorkOrder(Guid? sourceQuoteId = null, Guid? sourceAppointmentId = null) => WorkOrder.Create(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        null,
        Guid.NewGuid(),
        WorkOrderPriority.Normal,
        null,
        [new WorkOrderItemDefinition("Service", 1, 150, WorkOrderItemType.Service)],
        [],
        Now,
        sourceQuoteId,
        0,
        sourceAppointmentId);
}
